#include "RCONHelper.h"
#include <string>
#include <iostream>

RCONSocket* CreateObjectB(IPEndpoint& server, std::string &password)
{
    return new RCONSocket(server, password);
}

RCONSocket* CreateObjectA(IPEndpoint& server, std::string& password, std::string& command)
{
    return new RCONSocket(server, password, command);
}

int main(int argc, char const *argv[])
{
    IPEndpoint server;
    server.ip = "192.168.2.6";
    server.port = PORT;
    std::string password = "mukyumukyu";
    auto rcon = CreateObjectB(server, password);
    rcon->ExecuteSingle("list");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    rcon->ExecuteSingle("help");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    rcon->Execute("data get entity DoggySazHi");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    delete rcon;
    return 0;
}