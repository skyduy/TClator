#define CPPHTTPLIB_OPENSSL_SUPPORT
#define WIN32_LEAN_AND_MEAN

#include "tclator.h"

#include <Windows.h>
#include <httplib.h>
#include <openssl/sha.h>

#include <chrono>
#include <iostream>
#include <nlohmann/json.hpp>

using json = nlohmann::json;
using std::cout;
using std::endl;

const std::string SPLIT_TOKEN(1, 1);

std::string _calculate(std::string expression) {
    std::string res = expression;
    std::reverse(res.begin(), res.end());
    return res;
}

httplib::Params getPayload(const std::string& appKey, const std::string& appSecret,
                           const std::string& q) {
    auto now = std::chrono::duration_cast<std::chrono::milliseconds>(
                       std::chrono::system_clock::now().time_since_epoch())
                       .count();
    std::string curtime = std::to_string(now / 1000);
    std::string salt = std::to_string(now);

    std::string truncateQ = q;
    if (q.length() > 20) {
        truncateQ = q.substr(0, 10) + std::to_string(q.length()) + q.substr(q.length() - 10, 10);
    }
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
    cout << sign << endl;
    httplib::Params payload{
            {"from", "auto"}, {"to", "auto"},     {"signType", "v3"}, {"curtime", curtime},
            {"q", q},         {"appKey", appKey}, {"salt", salt},     {"sign", sign},
    };
    return payload;
}

std::string _translate(std::string src) {
    // 解析 appKey, appSecret, content
    size_t pos = 0;

    pos = src.find(SPLIT_TOKEN);
    std::string appKey = src.substr(0, pos);
    src.erase(0, pos + SPLIT_TOKEN.length());

    pos = src.find(SPLIT_TOKEN);
    std::string appSecret = src.substr(0, pos);
    src.erase(0, pos + SPLIT_TOKEN.length());

    std::string q = src;

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
    std::string answer;
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
    return answer;
}
