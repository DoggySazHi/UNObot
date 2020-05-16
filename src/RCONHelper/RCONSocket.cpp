#include "RCONSocket.h"
#include <string>
#include <iostream>

RCONSocket::RCONSocket(std::string& ip, std::string& password) : RCONSocket(ip, password, false)
{

}

RCONSocket::RCONSocket(std::string& ip, std::string& password, bool reuse) : RCONSocket(ip, password, reuse, NULL_STR)
{

}

RCONSocket::RCONSocket(std::string& ip, std::string& password, bool reuse, std::string& command)
{
    if(command == nullptr)
        std::cout << "gay" << std::endl;
}