#include "RCONHelper.h"
#include <iostream>
#include <cstdio>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <string>

void sendPacket(std::string& ip, int port)
{
    int sock, valread;
    struct sockaddr_in serv_addr{};
    std::string hello = "Hello from client";
    char buffer[BUFFER_SIZE] = {0};
    if ((sock = socket(AF_INET, SOCK_STREAM, 0)) < 0)
    {
        printf("\n Socket creation error \n");
        return;
    }

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(port);

    // Convert IPv4 and IPv6 addresses from text to binary form
    if(inet_pton(AF_INET, ip.c_str(), &serv_addr.sin_addr) <= 0)
    {
        printf("\nInvalid address/ Address not supported \n");
        return;
    }

    if (connect(sock, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0)
    {
        printf("\nConnection Failed \n");
        return;
    }
    send(sock , hello.c_str() , hello.length() , 0 );
    printf("Hello message sent\n");
    valread = read( sock , buffer, BUFFER_SIZE);
    printf("%s\n",buffer );

    std::cout << valread << std::endl;
}

void mukyu()
{
	for(int i = 0; i < 5; i++)
		std::cout << "Mukyu!" << std::endl;
}

int main(int argc, char const *argv[])
{
    std::string ip = "192.168.2.6";
    sendPacket(ip, PORT);
    return 0;
}