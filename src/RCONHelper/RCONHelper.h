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
	MODULE_API void mukyu();
// Stop exposing APIs here
	
#ifdef __cplusplus
}
#endif