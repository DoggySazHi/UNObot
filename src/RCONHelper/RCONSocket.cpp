#include "RCONSocket.h"
#include <string>
#include <cstring>
#include <iostream>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <array>

RCONSocket::RCONSocket(IPEndpoint& server, std::string& password) : RCONSocket(server, password, false)
{

}

RCONSocket::RCONSocket(IPEndpoint& server, std::string& password, bool reuse) : RCONSocket(server, password, reuse, NULL_STR)
{

}

RCONSocket::RCONSocket(IPEndpoint& server, std::string& password, bool reuse, std::string& command)
{
    this->server = server;
    this->socket_descriptor = 0;
    WipeBuffer();
    CreateConnection();
}

RCONSocket::~RCONSocket() {
    delete rx_data;
}

#pragma clang diagnostic push
#pragma ide diagnostic ignored "readability-convert-member-functions-to-static"
void RCONSocket::Mukyu()
{
    for(int i = 0; i < 5; i++)
        std::cout << "Mukyu!" << std::endl;
}
#pragma clang diagnostic pop

void RCONSocket::CreateConnection()
{
    struct sockaddr_in serv_addr{};
    std::string hello = "Hello from client";
    if ((socket_descriptor = socket(AF_INET, SOCK_STREAM, 0)) < 0)
    {
        std::cout << "Socket failure..." << '\n';
        return;
    }

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(server.port);

    // Convert IPv4 and IPv6 addresses from text to binary form
    if(inet_pton(AF_INET, server.ip.c_str(), &serv_addr.sin_addr) <= 0)
    {
        std::cout << "Address parsing failed..." << '\n';
        return;
    }

    if (connect(socket_descriptor, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0)
    {
        std::cout << "Connection failed..." << '\n';
        return;
    }

    SendPacket();
}

void RCONSocket::SendPacket()
{
    std::string hello = "Hello from client";

    send(socket_descriptor , hello.c_str() , hello.length() , 0 );
    std::cout << "Sent data." << '\n';
    int count = read(socket_descriptor , rx_data->data(), BUFFER_SIZE);
    std::cout << rx_data->data() << '\n';
    std::cout << "Count: " << count << '\n';
}

std::array<uint8_t, 4> RCONSocket::LittleEndianConverter(int data)
{
    auto b = std::array<uint8_t, 4>();
    b[0] = (uint8_t)data;
    b[1] = (uint8_t)(((uint)data >> 8) & 0xFF);
    b[2] = (uint8_t)(((uint)data >> 16) & 0xFF);
    b[3] = (uint8_t)(((uint)data >> 24) & 0xFF);
    return b;
}

int RCONSocket::LittleEndianReader(std::array<uint8_t, BUFFER_SIZE>* data, int startIndex)
{
    return (data->data()[startIndex + 3] << 24)
           | (data->data()[startIndex + 2] << 16)
           | (data->data()[startIndex + 1] << 8)
           | data->data()[startIndex];
}

void RCONSocket::WipeBuffer()
{
    memset(rx_data->data(), '\0', BUFFER_SIZE);
}

std::vector<uint8_t> RCONSocket::MakePacketData(std::string body, PacketType Type, int ID)
{
    auto Length = LittleEndianConverter(body.length() + 9);
    auto IDData = LittleEndianConverter(ID);
    auto PacketType = LittleEndianConverter(Type);
    auto BodyData = body.data();
    // Plus 1 for the null byte.
    auto Packet = std::vector<uint8_t>(Length.size() + IDData.size() + PacketType.size() + body.size());
    auto Counter = 0;
    for (auto Byte : Length)
        Packet[Counter++] = Byte;
    for (auto Byte : IDData)
        Packet[Counter++] = Byte;
    for (auto Byte : PacketType)
        Packet[Counter++] = Byte;
    for (auto Character : body)
        Packet[Counter++] = Character;
    return Packet;
}