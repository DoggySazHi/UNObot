#include <string>
#include "RCONSocket.h"

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
	MODULE_API inline void Mukyu(RCONSocket* obj) { obj->Mukyu(); }
    MODULE_API RCONSocket* CreateObjectA(std::string& ip, std::string& password, std::string& command);
    MODULE_API RCONSocket* CreateObjectB(std::string& ip, std::string& password);
// Stop exposing APIs here
	
#ifdef __cplusplus
}
#endif