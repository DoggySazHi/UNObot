#include <string>
#include <array>
#include <vector>

#define PORT 27286
#define BUFFER_SIZE 4096

enum RCONStatus {CONN_FAIL, AUTH_FAIL, EXEC_FAIL, INT_FAIL, SUCCESS};
enum PacketType { SERVERDATA_RESPONSE_VALUE = 0,  SERVERDATA_EXECCOMMAND = 2, SERVERDATA_AUTH = 3, TYPE_100 = 100};

struct IPEndpoint
{
    std::string ip;
    unsigned short port = PORT;
};

class RCONSocket {
    public:
        RCONSocket(IPEndpoint &server, std::string &password);
        RCONSocket(IPEndpoint &server, std::string &password, bool reuse);
        RCONSocket(IPEndpoint &server, std::string &password, bool reuse, std::string &command);
        ~RCONSocket();
        void Mukyu();

    private:
        IPEndpoint server;
        int socket_descriptor{};
        std::array<uint8_t, 4096>* rx_data = new std::array<uint8_t, BUFFER_SIZE>();
        std::string NULL_STR = "";
        static std::array<uint8_t, 4> LittleEndianConverter(int data);
        void CreateConnection();
        static int LittleEndianReader(std::array<uint8_t, BUFFER_SIZE>* data, int startIndex);
        static std::vector<uint8_t> MakePacketData(std::string body, PacketType Type, int ID);
        void WipeBuffer();
        void SendPacket();
};