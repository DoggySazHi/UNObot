#include <string>
#include <array>
#include <vector>

#define PORT 27286
#define BUFFER_SIZE 4096
#define MAX_PACKETS_READ 40

enum RCONStatus {CONN_FAIL, AUTH_FAIL, EXEC_FAIL, INT_FAIL, SUCCESS};
enum PacketType { SERVERDATA_RESPONSE_VALUE = 0,  SERVERDATA_EXECCOMMAND = 2, SERVERDATA_AUTH_RESPONSE = 2, SERVERDATA_AUTH = 3, TYPE_100 = 100};

struct IPEndpoint
{
    std::string ip;
    unsigned short port = PORT;
};

class RCONSocket {
    public:
        RCONStatus status;
        bool disposed{};
        std::string data;
        RCONSocket(IPEndpoint &server, std::string &password);
        RCONSocket(IPEndpoint &server, std::string &password, const std::string &command);
        ~RCONSocket();
        void Mukyu();
        void ExecuteSingle(const std::string& command);
        void Execute(const std::string &command);
        [[nodiscard]] bool IsConnected() const;
        void Dispose();

    IPEndpoint server;
private:
    std::string& password;
        int socket_descriptor;
        std::array<uint8_t, 4096>* rx_data = new std::array<uint8_t, BUFFER_SIZE>();
        static std::array<uint8_t, 4> LittleEndianConverter(int data);
        void CreateConnection(const std::string& command = "");
        static int LittleEndianReader(std::array<uint8_t, BUFFER_SIZE>* data, int start_index);
        static std::vector<uint8_t> MakePacketData(const std::string& body, PacketType type, int id);
        void WipeBuffer();
        bool Authenticate();
};