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

RCONSocket* CreateObjectA(char* ip, ushort port, char* password, char* command)
{
    IPEndpoint server;
    server.ip = std::string(ip);
    server.port = port;
#ifndef NDEBUG
    std::cout << "Server input: " << ip << " Stringified: " << server.ip << '\n';
#endif
    std::string str_pwd(password);
    std::string str_cmd(command);
    return CreateObjectA(server, str_pwd, str_cmd);
}

RCONSocket* CreateObjectB(char* ip, ushort port, char* password)
{
    IPEndpoint server;
    server.ip = std::string(ip);
    server.port = port;
#ifndef NDEBUG
    std::cout << "Server input: " << ip << " Stringified: " << server.ip << '\n';
#endif
    std::string str_pwd(password);
    return CreateObjectB(server, str_pwd);
}

int main()
{
    IPEndpoint server;
    server.ip = "127.0.0.1";
    server.port = 29293;
    std::string password = "mukyumukyu";
    auto rcon = CreateObjectB(server, password);
    rcon->ExecuteSingle("list");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    rcon->Execute("data get entity DoggySazHi");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    rcon->Execute("data get entity PuppySazHi");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    rcon->Execute("data get entity MyonSazHi");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    rcon->Execute("data get entity d3kuu");
    std::cout << rcon->status << '\n';
    std::cout << rcon->data << '\n';
    delete rcon;
    return 0;
}