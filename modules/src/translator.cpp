

#define CPPHTTPLIB_OPENSSL_SUPPORT
#define WIN32_LEAN_AND_MEAN

#include "translator.h"

#include <Windows.h>
#include <httplib.h>
#include <openssl/sha.h>

#include <chrono>
#include <iostream>
#include <nlohmann/json.hpp>
#include <vector>
using json = nlohmann::json;
using std::cout;
using std::endl;

inline std::string truncate(const std::string& text, int length) {
    std::vector<std::string> chars;
    for (size_t i = 0; i < text.length();) {
        int cplen = 1;
        if ((text[i] & 0xf8) == 0xf0)
            cplen = 4;
        else if ((text[i] & 0xf0) == 0xe0)
            cplen = 3;
        else if ((text[i] & 0xe0) == 0xc0)
            cplen = 2;
        if ((i + cplen) > text.length()) cplen = 1;
        chars.push_back(text.substr(i, cplen));
        i += cplen;
    }

    std::string ans;
    if (chars.size() > 20) {
        for (size_t i = 0; i < 10; i++) {
            ans += chars[i];
        }
        ans += std::to_string(chars.size());
        for (size_t i = chars.size() - 10; i < chars.size(); i++) {
            ans += chars[i];
        }
    } else {
        ans = text;
    }
    return ans;
}

httplib::Params getPayload(const std::string& appKey, const std::string& appSecret,
                           const std::string& q) {
    auto now = std::chrono::duration_cast<std::chrono::milliseconds>(
                       std::chrono::system_clock::now().time_since_epoch())
                       .count();
    std::string curtime = std::to_string(now / 1000);
    std::string salt = std::to_string(now);
    std::string truncateQ = truncate(q, 20);

    std::string signStr = appKey + truncateQ + salt + curtime + appSecret;

    unsigned char hash[SHA256_DIGEST_LENGTH];
    SHA256_CTX sha256;
    SHA256_Init(&sha256);
    SHA256_Update(&sha256, signStr.c_str(), signStr.size());
    SHA256_Final(hash, &sha256);
    std::stringstream ss;
    for (int i = 0; i < SHA256_DIGEST_LENGTH; i++) {
        ss << std::hex << std::setw(2) << std::setfill('0') << (int)hash[i];
    }
    std::string sign = ss.str();
    httplib::Params payload{
            {"from", "auto"}, {"to", "auto"},     {"signType", "v3"}, {"curtime", curtime},
            {"q", q},         {"appKey", appKey}, {"salt", salt},     {"sign", sign},
    };
    return payload;
}

std::string Translator::query(std::string src) {
    std::string answer;

    // 解析 appKey, appSecret, content
    size_t pos = 0;

    pos = src.find(SPLIT_TOKEN);
    std::string appKey = src.substr(0, pos);
    src.erase(0, pos + SPLIT_TOKEN.length());

    pos = src.find(SPLIT_TOKEN);
    std::string appSecret = src.substr(0, pos);
    src.erase(0, pos + SPLIT_TOKEN.length());

    std::string q = src;
    if (cache.get(q, answer)) {
        return answer;
    }

    // http请求
    httplib::Client cli("openapi.youdao.com");
    auto payLoad = getPayload(appKey, appSecret, q);
    auto res = cli.Post("/api", payLoad);

    // 错误处理
    if (res->status != 200) {
        return "Status Code: " + std::to_string(res->status);
    }
    auto o = json::parse(res->body);
    std::string errorCode = o["errorCode"].get<std::string>();
    if (errorCode != "0") {
        switch (std::stoi(errorCode)) {
        case 108:
            return "[有道智云] 应用ID不正确（请右击托盘图标设置）";
        case 202:
            return "[有道智云] 签名检验失败（请右击托盘图标设置）";
        default:
            return "[有道智云] 错误代码 " + errorCode;
        }
    }

    // 构造答案
    if (o.contains("translation")) {
        if (o.contains("basic") && o["basic"].contains("phonetic")) {
            answer += "[" + o["basic"]["phonetic"].get<std::string>() + "] ";
        }
        for (auto& ele : o["translation"]) {
            answer += ele.get<std::string>();
            answer += SPLIT_TOKEN;
        }
    }
    if (o.contains("basic") && o["basic"].contains("explains")) {
        for (auto& ele : o["basic"]["explains"]) {
            answer += ele.get<std::string>();
            answer += SPLIT_TOKEN;
        }
    }
    if (o.contains("web")) {
        for (auto& pair : o["web"]) {
            if (pair["key"].get<std::string>() == q) {
                continue;
            }
            answer += pair["key"].get<std::string>() + ": ";
            for (size_t i = 0; i < pair["value"].size(); i++) {
                if (i != 0) {
                    answer += " | ";
                }
                answer += pair["value"][i].get<std::string>();
            }
            answer += SPLIT_TOKEN;
        }
    }

    answer.pop_back(); // delete last SPLIT_TOKEN
    cache.put(q, answer);
    return answer;
}
