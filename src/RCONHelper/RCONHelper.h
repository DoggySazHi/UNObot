#include <string>
#include <iostream>
#include "RCONSocket.h"
#ifdef RCONLIB
#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#  ifdef MODULE_API_EXPORTS
#    define MODULE_API __declspec(dllexport)
#  else
#    define MODULE_API __declspec(dllimport)
#  endif
#else
#  define MODULE_API
#endif

// Start exposing APIs here
    MODULE_API RCONSocket* CreateObjectA(const char* ip, unsigned short port, const char* password, const char* command);
    MODULE_API RCONSocket* CreateObjectB(const char* ip, unsigned short port, const char* password);
    MODULE_API void DestroyObject(RCONSocket* obj) { delete obj; }

    MODULE_API void MukyuN() { std::cout << "Mukyu!" << '\n'; }
    MODULE_API void Mukyu(RCONSocket* obj) { obj->Mukyu(); }
    MODULE_API const char* Say(char* thing) { std::cout << thing << '\n'; return new char[5] {'h', 'e', 'w', 'o', '\0'}; }
    MODULE_API void SayDelete(const char* thing) { delete thing; }

    MODULE_API RCONStatus GetStatus(RCONSocket* obj) { return obj->status; }
    MODULE_API bool Disposed(RCONSocket* obj) { return obj->disposed; }
    MODULE_API const char* GetData(RCONSocket* obj) { return obj->data.c_str(); }
    MODULE_API bool Connected(RCONSocket* obj) { return obj->IsConnected(); }
    MODULE_API void ExecuteSingle(RCONSocket* obj, char* command) { obj->ExecuteSingle(command); }
    MODULE_API void Execute(RCONSocket* obj, char* command) { obj->Execute(command); }
    MODULE_API void Dispose(RCONSocket* obj) { obj->Dispose(); }
    MODULE_API const char* GetServerIP(RCONSocket* obj) { return obj->server.ip.c_str(); }
    MODULE_API unsigned short GetServerPort(RCONSocket* obj) { return obj->server.port; }
// Stop exposing APIs here
	
#ifdef __cplusplus
}
#endif
#endif

#ifdef __cplusplus
class RCONHelper {
public:
    static RCONSocket* CreateObjectA(const char* ip, unsigned short port, const char* password, const char* command);
    static RCONSocket* CreateObjectB(const char* ip, unsigned short port, const char* password);
    static void DestroyObject(RCONSocket* obj) { delete obj; }

    static void MukyuN() { std::cout << "Mukyu!" << '\n'; }
    static void Mukyu(RCONSocket* obj) { obj->Mukyu(); }
    static const char* Say(char* thing) { std::cout << thing << '\n'; return new char[5] {'h', 'e', 'w', 'o', '\0'}; }
    static void SayDelete(const char* thing) { delete thing; }

    static RCONStatus GetStatus(RCONSocket* obj) { return obj->status; }
    static bool Disposed(RCONSocket* obj) { return obj->disposed; }
    static const char* GetData(RCONSocket* obj) { return obj->data.c_str(); }
    static bool Connected(RCONSocket* obj) { return obj->IsConnected(); }
    static void ExecuteSingle(RCONSocket* obj, char* command) { obj->ExecuteSingle(command); }
    static void Execute(RCONSocket* obj, char* command) { obj->Execute(command); }
    static void Dispose(RCONSocket* obj) { obj->Dispose(); }
    static const char* GetServerIP(RCONSocket* obj) { return obj->server.ip.c_str(); }
    static unsigned short GetServerPort(RCONSocket* obj) { return obj->server.port; }
};
#endif