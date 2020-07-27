#include "RCONHelper.h"
#include <string>
#include <iostream>
#ifndef SIGPIPE
    #include <csignal>
#endif
#include <chrono>
#include <thread>

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
    signal(SIGPIPE, SIG_IGN);

    IPEndpoint server;
    server.ip = "192.168.2.11";
    server.port = 29293;
    std::string password = "mukyumukyu";
    auto rcon = CreateObjectB(server, password);
    for(int i = 0; i < 209; i++) {
        for(int j = 0; j < 8; j++)
        {
            rcon->ExecuteSingle("execute as DoggySazHi at @s run tp @s ~ ~ ~-1");
            rcon->ExecuteSingle("clear DoggySazHi minecraft:rail 1");
            rcon->ExecuteSingle("execute as DoggySazHi at @s run setblock ~ ~ ~ rail keep");
            std::this_thread::sleep_until(std::chrono::system_clock::now() + std::chrono::milliseconds(30));
        }
        rcon->ExecuteSingle("execute as DoggySazHi at @s run tp @s ~ ~ ~-1");
    }
    delete rcon;
    return 0;
}