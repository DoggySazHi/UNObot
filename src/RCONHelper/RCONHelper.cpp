#include "RCONHelper.h"
#include <string>

RCONSocket* CreateObjectC(IPEndpoint& server, std::string& password)
{
    return new RCONSocket(server, password);
}

RCONSocket* CreateObjectB(IPEndpoint& server, std::string &password, bool reuse)
{
    return new RCONSocket(server, password, reuse);
}

RCONSocket* CreateObjectA(IPEndpoint& server, std::string& password, bool reuse, std::string& command)
{
    return new RCONSocket(server, password, reuse, command);
}

int main(int argc, char const *argv[])
{
    IPEndpoint server;
    server.ip = "192.168.2.6";
    server.port = PORT;
    std::string password = "mukyumukyu";
    CreateObjectC(server, password);
    return 0;
}