#include <string>

class RCONSocket {
    public:
        RCONSocket(std::string& ip, std::string& password, bool reuse, std::string& command);
        RCONSocket(std::string &ip, std::string &password, bool reuse);
        RCONSocket(std::string &ip, std::string &password);

    enum RCONStatus {CONN_FAIL, AUTH_FAIL, EXEC_FAIL, INT_FAIL, SUCCESS};
    private:
        std::string NULL_STR = "";
        enum PacketType {/* SERVERDATA_RESPONSE_VALUE = 0, */ SERVERDATA_EXECCOMMAND = 2, SERVERDATA_AUTH = 3, TYPE_100 = 100};
};