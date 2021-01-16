#include "RCONSocket.h"
#include "Utilities.h"
#include <string>
#include <cstring>
#include <iostream>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <array>
#ifndef SIGPIPE
    #include <sys/signal.h>
#endif

// Because it exists on LINUX, but not on MacOS or BSD. Luckily, Windows does not care.
#ifndef MSG_NOSIGNAL
    #define MSG_NOSIGNAL 0
#endif

RCONSocket::RCONSocket(IPEndpoint& server, std::string& password) : RCONSocket(server, password, "")
{

}

RCONSocket::RCONSocket(IPEndpoint& server, std::string& password, const std::string& command) : password(password)
{
    status = CONN_FAIL;
    this->server = server;
    this->socket_descriptor = 0;
    WipeBuffer();
    CreateConnection(command);
}

RCONSocket::~RCONSocket() {
    if(!disposed)
        Dispose();
}

void RCONSocket::Mukyu()
{
    for(int i = 0; i < 5; i++)
        std::cout << "Mukyu!" << std::endl;
}

void RCONSocket::CreateConnection(const std::string& command)
{
    struct sockaddr_in serv_addr{};
    if ((socket_descriptor = socket(AF_INET, SOCK_STREAM, 0)) < 0)
    {
        std::cerr << "Socket failure..." << '\n';
        return;
    }

    // Allow for the reusing of addresses (ESPECIALLY IF IT FORGETS TO CLOSE)
    int yes = 1; // ok, why do you want a pointer to a true
    auto success = setsockopt(socket_descriptor , SOL_SOCKET, SO_REUSEADDR, &yes, sizeof(int)) == 0;

    // :reimuthink: stupid SIGPIPE keep crashing UNObot (MacOS/BSD)
#ifdef SO_NOSIGPIPE
    success |= setsockopt (socket_descriptor, SOL_SOCKET, SO_NOSIGPIPE, &yes, sizeof(int)) == 0;
#endif

    // Set the timeouts
    struct timeval timeout {};
    timeout.tv_sec = 5;
    timeout.tv_usec = 0;

    success |= setsockopt (socket_descriptor, SOL_SOCKET, SO_RCVTIMEO, (char *)&timeout, sizeof(timeout)) == 0;
    success |= setsockopt (socket_descriptor, SOL_SOCKET, SO_SNDTIMEO, (char *)&timeout, sizeof(timeout)) == 0;

    if(!success)
        std::cerr << "Failed to set socket options!" << '\n';

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(server.port);

    // Convert IPv4 and IPv6 addresses from text to binary form
    if(inet_pton(AF_INET, server.ip.c_str(), &serv_addr.sin_addr) <= 0)
    {
        std::cerr << "Address parsing failed... IP: " << server.ip << " Port: " << server.port << '\n';
        return;
    }

    if (connect(socket_descriptor, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0)
    {
        std::cerr << "Failed to connect to " << server.ip << " at " << server.port << ".\n";
        return;
    }

    std::cout << "Successfully created RCON connection!" << '\n';

    if(Authenticate() && !command.empty())
        ExecuteSingle(command);
}

bool RCONSocket::IsConnected() const
{
    int error = 0;
    socklen_t len = sizeof(error);
    int success = getsockopt(socket_descriptor, SOL_SOCKET, SO_ERROR, &error, &len);

    if (success != 0)
    {
        std::cerr << "Error getting errors for socket: " << strerror(success) << '\n';
        return false;
    }

    if (error != 0)
    {
        std::cerr << "Socket error: " << strerror(error) << '\n';
        return false;
    }

    return true;
}

bool RCONSocket::Authenticate()
{
    auto payload = MakePacketData(password, SERVERDATA_AUTH, 0);
    send(socket_descriptor, payload.data(), payload.size(), MSG_NOSIGNAL);
#ifndef NDEBUG
    Utilities::hexdump(payload.data(), payload.size());
#endif
    auto count = read(socket_descriptor, rx_data->data(), BUFFER_SIZE);
    auto id = LittleEndianReader(rx_data, 4);
    auto type = LittleEndianReader(rx_data, 8);
    if (count < 12 || id == -1 || type != SERVERDATA_AUTH_RESPONSE)
    {
        status = AUTH_FAIL;
        std::cerr <<  "RCON failed to authenticate!" << '\n';
        return false;
    }
    std::cout << "RCON login successful!" << '\n';
    status = SUCCESS;
    return true;
}

void RCONSocket::ExecuteSingle(const std::string& command)
{
    if(status == AUTH_FAIL)
        Authenticate();
    if(status == AUTH_FAIL)
    {
        std::cerr << "Failed to re-authenticate!" << '\n';
        return;
    }

    WipeBuffer();
    auto payload = MakePacketData(command, SERVERDATA_EXECCOMMAND, 0);
    send(socket_descriptor, payload.data(), payload.size(), MSG_NOSIGNAL);
    int count = read(socket_descriptor, rx_data->data(), BUFFER_SIZE);
    auto id = LittleEndianReader(rx_data, 4);
    auto type = LittleEndianReader(rx_data, 8);
    if(id == -1 || type != SERVERDATA_RESPONSE_VALUE || count <= 12)
    {
        std::cerr << "Failed to execute \"" << command << "\"!" << '\n';
        status = INT_FAIL;
        return;
    }
    data.clear();
    data.reserve(count - 12);
    auto position = 12;
    auto current_char = (char) (*rx_data)[position++];
    while (current_char != '\x00')
    {
        data += current_char;
        current_char = (char) (*rx_data)[position++];
    }
    status = SUCCESS;
}

void RCONSocket::ExecuteFast(const std::vector<uint8_t>& payload)
{
    if(status == AUTH_FAIL)
        Authenticate();
    if(status == AUTH_FAIL)
    {
        std::cerr << "Failed to re-authenticate!" << '\n';
        return;
    }

    send(socket_descriptor, payload.data(), payload.size(), MSG_NOSIGNAL);
    int count = read(socket_descriptor, rx_data->data(), BUFFER_SIZE);
    auto id = LittleEndianReader(rx_data, 4);
    auto type = LittleEndianReader(rx_data, 8);
    if(id == -1 || type != SERVERDATA_RESPONSE_VALUE || count <= 12)
    {
        std::cerr << "Fast execute failed!" << '\n';
        status = INT_FAIL;
        return;
    }
    status = SUCCESS;
}

void RCONSocket::Execute(const std::string& command)
{
    if(status == AUTH_FAIL)
        Authenticate();
    if(status == AUTH_FAIL)
    {
        std::cerr << "Failed to re-authenticate!" << '\n';
        return;
    }

    auto packet_count = 0;
    auto payload = MakePacketData(command, SERVERDATA_EXECCOMMAND, 0);
    auto end_of_command = MakePacketData("", TYPE_100, 0);
    send(socket_descriptor, payload.data(), payload.size(), MSG_NOSIGNAL);
    //TODO fix SIGPIPE
    send(socket_descriptor, end_of_command.data(), end_of_command.size(), MSG_NOSIGNAL);

    data.clear();
    WipeBuffer();
    int count = read(socket_descriptor, rx_data->data(), BUFFER_SIZE);
#ifndef NDEBUG
    Utilities::hexdump(rx_data->data(), count);
    Utilities::file_dump(rx_data->data(), count, "packet" + std::to_string(packet_count));
#endif
    std::cout << "Planning to execute \"" << command << "\"!" << '\n';
    std::cout << "Connection status: " << IsConnected() << '\n';

    int dataTrim = -1;
    int startOfPacket = 0;
    int lifetime = 0;
    int position = 0;
    while (count > 0)
    {
        packet_count++;

#ifndef NDEBUG
        std::cout << "reading packet " << packet_count << '\n';
#endif

        // Keep track of whenever a new packet starts
        if(lifetime == 0) {
            // This tells us how many characters we should read (ID + Type removed, plus 2 null chars)
            lifetime = LittleEndianReader(rx_data, startOfPacket) - 10;
            position = startOfPacket + 12;
            data.reserve(data.length() + lifetime);
#ifndef NDEBUG
            std::cerr << "Position: " << position << " Lifetime: " << lifetime << '\n';
#endif
        }

        if(packet_count == 1) {
            auto id = LittleEndianReader(rx_data, 4);
            auto type = LittleEndianReader(rx_data, 8);
            if(id == -1 || type != SERVERDATA_RESPONSE_VALUE || count <= 12)
            {
                std::cerr << "Failed to execute \"" << command << "\"!" << '\n';
                status = EXEC_FAIL;
                return;
            }
        }

        auto current_char = (char) (*rx_data)[position++];
        while(position < count && lifetime > 0)
        {
            lifetime--;
            if(current_char != '\x00')
                data += current_char;
            current_char = (char) (*rx_data)[position++];
        }

        if(lifetime == 0) {
            if(position != count - 1)
            {
                startOfPacket = position;
                continue;
            }
            startOfPacket = 0;
        }

        // This is the purpose of the end_of_command, so we read for the error to stop reading (0x64 = 100)
        dataTrim = data.find("Unknown request 64");
        if (dataTrim >= 0)
            break;

        if(packet_count >= MAX_PACKETS_READ)
        {
            std::cerr << "Overread " << packet_count << " packets!" << '\n';
            break;
        }

        WipeBuffer();
        count = read(socket_descriptor, rx_data->data(), BUFFER_SIZE);
#ifndef NDEBUG
        Utilities::hexdump(rx_data->data(), count);
        Utilities::file_dump(rx_data->data(), count, "packet" + std::to_string(packet_count));
#endif
        position = 0;
    }
    if(count < 0)
    {
        status = INT_FAIL;
        std::cerr << "Socket errored! (" << count << "): " << strerror(errno) << '\n';
        if(packet_count >= 2)
        {
            std::cerr << "There is data read, therefore will be marked as a success." << '\n';
            status = SUCCESS;
        }
    }
    else
        status = SUCCESS;
    std::cout << "Finished reading data." << '\n';
    std::cout << "Read " << packet_count << " packets!" << '\n';
    if(dataTrim >= 0)
        data.erase(dataTrim);
}

// Function works. Stop telling me to not play with unsigned numbers.
std::array<uint8_t, 4> RCONSocket::LittleEndianConverter(int data)
{
    auto b = std::array<uint8_t, 4>();
    if(!Utilities::is_big_endian())
    {
        b[0] = (uint8_t) data;
        b[1] = (uint8_t) (((uint) data >> 8) & 0xFF);
        b[2] = (uint8_t) (((uint) data >> 16) & 0xFF);
        b[3] = (uint8_t) (((uint) data >> 24) & 0xFF);
    }
    else
    {
        b[3] = (uint8_t)data;
        b[2] = (uint8_t)(((uint)data >> 8) & 0xFF);
        b[1] = (uint8_t)(((uint)data >> 16) & 0xFF);
        b[0] = (uint8_t)(((uint)data >> 24) & 0xFF);
    }
    return b;
}

int RCONSocket::LittleEndianReader(std::array<uint8_t, BUFFER_SIZE>* data, int start_index)
{
    if(!Utilities::is_big_endian())
        return ((*data)[start_index + 3] << 24)
               | ((*data)[start_index + 2] << 16)
               | ((*data)[start_index + 1] << 8)
               | (*data)[start_index];
    return ((*data)[start_index] << 24)
           | ((*data)[start_index + 1] << 16)
           | ((*data)[start_index + 2] << 8)
           | (*data)[start_index + 3];
}

void RCONSocket::WipeBuffer()
{
    memset(rx_data->data(), '\0', BUFFER_SIZE);
}

std::vector<uint8_t> RCONSocket::MakePacketData(const std::string& body, PacketType type, int id) {
    int length_num = body.length(); // idk why it goes to unsigned long with auto
    auto length = LittleEndianConverter(length_num + 10);
    auto id_data = LittleEndianConverter(id);
    auto packet_type = LittleEndianConverter(type);
    // Plus 1 for the null byte.
    auto packet = std::vector<uint8_t>(length.size() + id_data.size() + packet_type.size() + body.size() + 2);
    auto counter = 0;
    for (auto byte : length)
        packet[counter++] = byte;
    for (auto byte : id_data)
        packet[counter++] = byte;
    for (auto byte : packet_type)
        packet[counter++] = byte;
    for (auto character : body)
        packet[counter++] = character;
    packet[counter++] = '\x00';
    packet[counter++] = '\x00';
    return packet;
}

void RCONSocket::Dispose()
{
    close(socket_descriptor);
    delete rx_data;
    std::cout << "Closed socket." << '\n';
}