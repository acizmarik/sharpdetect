// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "../lib/optional/include/tl/optional.hpp"
#include "../lib/msgpack-c/include/msgpack.hpp"

namespace msgpack {
MSGPACK_API_VERSION_NAMESPACE(MSGPACK_DEFAULT_API_NS) {
namespace adaptor {

template <typename T>
struct as<tl::optional<T>, typename std::enable_if<msgpack::has_as<T>::value>::type> {
    tl::optional<T> operator()(msgpack::object const& o) const {
        if(o.is_nil()) return tl::nullopt;
        return o.as<T>();
    }
};

template <typename T>
struct convert<tl::optional<T> > {
    msgpack::object const& operator()(msgpack::object const& o, tl::optional<T>& v) const {
        if(o.is_nil()) v = tl::nullopt;
        else {
            T t;
            msgpack::adaptor::convert<T>()(o, t);
            v = t;
        }
        return o;
    }
};

template <typename T>
struct pack<tl::optional<T> > {
    template <typename Stream>
    msgpack::packer<Stream>& operator()(msgpack::packer<Stream>& o, const tl::optional<T>& v) const {
        if (v) o.pack(*v);
        else o.pack_nil();
        return o;
    }
};

template <typename T>
struct object<tl::optional<T> > {
    void operator()(msgpack::object& o, const tl::optional<T>& v) const {
        if (v) msgpack::adaptor::object<T>()(o, *v);
        else o.type = msgpack::type::NIL;
    }
};

template <typename T>
struct object_with_zone<tl::optional<T> > {
    void operator()(msgpack::object::with_zone& o, const tl::optional<T>& v) const {
        if (v) msgpack::adaptor::object_with_zone<T>()(o, *v);
        else o.type = msgpack::type::NIL;
    }
};

} // namespace adaptor
} // MSGPACK_API_VERSION_NAMESPACE
} // namespace msgpack

