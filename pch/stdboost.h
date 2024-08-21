// stdboost.h : include file for boost include files

#pragma once

#include "zou/pch/stdafx.h"

// http://stackoverflow.com/questions/18837401/c-boost-read-json-crash-and-i-had-define-boost-spirit-threadsafe
// http://www.boost.org/doc/libs/1_60_0/boost/log/support/spirit_classic.hpp
#define BOOST_SPIRIT_THREADSAFE

// Since boost v1.62.0.0, coroutine is deprecated
#define BOOST_COROUTINE_NO_DEPRECATION_WARNING
#define BOOST_COROUTINES_NO_DEPRECATION_WARNING

#pragma warning(push, 0)

#include <boost/algorithm/string.hpp>
#include <boost/algorithm/string/join.hpp>
#include <boost/archive/iterators/binary_from_base64.hpp>
#include <boost/archive/iterators/base64_from_binary.hpp>
#include <boost/archive/iterators/dataflow_exception.hpp>
#include <boost/archive/iterators/transform_width.hpp>
#include <boost/date_time.hpp>
#include <boost/exception/exception.hpp>
#include <boost/filesystem.hpp>
#include <boost/format.hpp>
#include <boost/function/function_base.hpp>
#include <boost/functional/hash.hpp>
#include <boost/interprocess/windows_shared_memory.hpp>
#include <boost/lexical_cast.hpp>
#include <boost/locale.hpp>
// avoid warning STL4038: The contents of <stdfloat> are available only with C++23 or later.
//#include <boost/multiprecision/cpp_dec_float.hpp>
#include <boost/property_tree/json_parser.hpp>
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/xml_parser.hpp>
#include <boost/process.hpp>
#include <boost/process/v1/windows.hpp>
#include <boost/regex.hpp>
#include <boost/signals2.hpp>
#include <boost/signals2/signal.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition_variable.hpp>
#include <boost/tokenizer.hpp>
#include <boost/utility/string_ref.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include <boost/uuid/uuid_io.hpp>

#pragma warning(pop)
