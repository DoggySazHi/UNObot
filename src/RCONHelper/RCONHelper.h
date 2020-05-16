#include <string>

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
    const unsigned short PORT = 27286;
    const int BUFFER_SIZE = 4096;
	MODULE_API void mukyu();
// Stop exposing APIs here
	
#ifdef __cplusplus
}
#endif

void sendPacket(std::string& ip, int port);