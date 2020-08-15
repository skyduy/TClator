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

const std::string SPLIT_TOKEN(1, 1);

std::string _calculate(std::string expression) {
    std::string res = expression;
    std::reverse(res.begin(), res.end());
    return res;
}

std::string sha256(const std::string str) {
    char buf[2];
    unsigned char hash[SHA256_DIGEST_LENGTH];
    SHA256_CTX sha256;
    SHA256_Init(&sha256);
    SHA256_Update(&sha256, str.c_str(), str.size());
    SHA256_Final(hash, &sha256);
    std::string NewString = "";
    for (int i = 0; i < SHA256_DIGEST_LENGTH; i++) {
        sprintf(buf, "%02x", hash[i]);
        NewString = NewString + buf;
    }
    return NewString;
}

httplib::Params constructPayload(std::string& src) {
    // parse appKey, appSecret, content
    size_t pos = 0;

    pos = src.find(SPLIT_TOKEN);
    std::string appKey = src.substr(0, pos);
    src.erase(0, pos + SPLIT_TOKEN.length());

    pos = src.find(SPLIT_TOKEN);
    std::string appSecret = src.substr(0, pos);
    src.erase(0, pos + SPLIT_TOKEN.length());

    std::string q = src;

    // construct payload
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
    std::string sign = signStr;
    httplib::Params payload{
            {"from", "auto"}, {"to", "auto"},     {"signType", "v3"}, {"curtime", curtime},
            {"q", q},         {"appKey", appKey}, {"salt", salt},     {"sign", sign},
    };
    return payload;
}

std::string _translate(std::string src) {
    auto payLoad = constructPayload(src);

    httplib::SSLClient cli("https://www.baidu.com");

    if (auto res = cli.Get("/")) {
        cli.get_openssl_verify_result();
    }
    // auto res = json::parse(r.text);
    // switch (res["errorCode"]) {
    // case "108":
    //    return "[有道智云] 应用ID不正确（请右击托盘图标设置）";
    // case "202":
    //    return "[有道智云] 签名检验失败（请右击托盘图标设置）";
    // default:
    //    return "[有道智云] 错误代码" + res["errorCode"];
    //}

    return "";
}

int main() {
    std::string appKey = "1";
    std::string appSecret = "2";
    std::cout << _translate(appKey + SPLIT_TOKEN + appSecret + SPLIT_TOKEN + "今天周几")
              << std::endl;
}
