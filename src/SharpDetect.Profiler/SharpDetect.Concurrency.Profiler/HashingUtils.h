// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <functional>
#include <tuple>
#include <utility>

namespace Profiler
{
    template <typename T>
    inline void hash_combine(std::size_t& seed, const T& value)
    {
        std::hash<T> hasher;
        seed ^= hasher(value) + 0x9e3779b9 + (seed << 6) + (seed >> 2);
    }

    template <typename Tuple, std::size_t... Is>
    void hash_tuple(std::size_t& seed, const Tuple& tuple, std::index_sequence<Is...>)
    {
        (void)std::initializer_list<int>{
            (hash_combine(seed, std::get<Is>(tuple)), 0)...};
    }

    template <typename T1, typename T2>
    struct pair_hash {
        std::size_t operator()(const std::pair<T1, T2>& p) const
        {
            std::size_t seed = 0;
            hash_combine(seed, p.first);
            hash_combine(seed, p.second);
            return seed;
        }
    };

    template <typename... Types>
    struct tuple_hash {
        std::size_t operator()(const std::tuple<Types...>& t) const
        {
            std::size_t seed = 0;
            hash_tuple(seed, t, std::make_index_sequence<sizeof...(Types)>{});
            return seed;
        }
    };
}
