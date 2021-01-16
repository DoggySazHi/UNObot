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

inline RCONSocket* CreateObjectA(const char* ip, const unsigned short port, const char* password, const char* command)
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

inline RCONSocket* CreateObjectB(const char* ip, const unsigned short port, const char* password)
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

RCONSocket *RCONHelper::CreateObjectA(const char *ip, const unsigned short port, const char *password, const char *command) {
    IPEndpoint server;
    server.ip = std::string(ip);
    server.port = port;
#ifndef NDEBUG
    std::cout << "Server input: " << ip << " Stringified: " << server.ip << '\n';
#endif
    std::string str_pwd(password);
    std::string str_cmd(command);
    return new RCONSocket(server, str_pwd, str_cmd);
}

RCONSocket *RCONHelper::CreateObjectB(const char *ip, const unsigned short port, const char *password) {
    IPEndpoint server;
    server.ip = std::string(ip);
    server.port = port;
#ifndef NDEBUG
    std::cout << "Server input: " << ip << " Stringified: " << server.ip << '\n';
#endif
    std::string str_pwd(password);
    return new RCONSocket(server, str_pwd);
}
