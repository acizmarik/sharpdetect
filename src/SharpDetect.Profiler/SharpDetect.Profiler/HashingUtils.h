// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <functional>
#include <tuple>
#include <utility>

namespace Profiler
{
    template <typename T>
    inline void hash_combine(std::size_t& seed, const T& value) {
        std::hash<T> hasher;
        seed ^= hasher(value) + 0x9e3779b9 + (seed << 6) + (seed >> 2);
    }

    template <typename Tuple, std::size_t... Is>
    void hash_tuple(std::size_t& seed, const Tuple& tuple, std::index_sequence<Is...>) {
        (hash_combine(seed, std::get<Is>(tuple)), ...);
    }

    template <typename T1, typename T2>
    struct pair_hash {
        std::size_t operator()(const std::pair<T1, T2>& p) const {
            std::size_t seed = 0;
            hash_combine(seed, p.first);
            hash_combine(seed, p.second);
            return seed;
        }
    };

    template <typename... Types>
    struct tuple_hash {
        std::size_t operator()(const std::tuple<Types...>& t) const {
            std::size_t seed = 0;
            hash_tuple(seed, t, std::index_sequence_for<Types...>{});
            return seed;
        }
    };
}