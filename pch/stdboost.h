// stdboost.h : include file for boost include files
// Include the following MSBuild code at the end of associated C++ project (.vcxproj)
// 
//  <ImportGroup Label = "ExtensionTargets">
//    <Import Project = "$(ZouDir)Zou.Cpp.targets" / >
//  </ ImportGroup>


#pragma once

#include <boost/algorithm/string.hpp>
#include <boost/algorithm/string/predicate.hpp>
#include <boost/archive/iterators/binary_from_base64.hpp>
#include <boost/archive/iterators/base64_from_binary.hpp>
#include <boost/archive/iterators/transform_width.hpp>
#include <boost/asio.hpp>
#include <boost/asio/spawn.hpp>
#include <boost/date_time.hpp>
#include <boost/filesystem.hpp>
#include <boost/filesystem/path.hpp>
#include <boost/format.hpp>
#include <boost/functional/hash.hpp>
#include <boost/lexical_cast.hpp>
#include <boost/locale.hpp>
#include <boost/multiprecision/cpp_dec_float.hpp>
#include <boost/property_tree/json_parser.hpp>
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/xml_parser.hpp>
#include <boost/regex.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition_variable.hpp>
#include <boost/tokenizer.hpp>
#include <boost/utility/string_ref.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include <boost/uuid/uuid_io.hpp>

// Disable some boost 1.70.0 warnings
#pragma warning(push)
#pragma warning(disable : 4244) // disable "conversion....possible loss of data" warning
#include <boost/process/detail/windows/wait_group.hpp>
#pragma warning(pop)
