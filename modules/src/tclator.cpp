#define CPPHTTPLIB_OPENSSL_SUPPORT
#define WIN32_LEAN_AND_MEAN

#include "tclator.h"

#include <Windows.h>
#include <httplib.h>
#include <openssl/sha.h>

#include <chrono>
#include <cmath>
#include <iostream>
#include <nlohmann/json.hpp>
#include <stack>
#include <vector>

using json = nlohmann::json;
using std::cout;
using std::endl;

const std::string SPLIT_TOKEN(1, 1);

inline int getPriority(char operate) //栈内优先级
{
    switch (operate) {
    case '+':
    case '-':
        return 2;
    case '*':
    case '/':
        return 3;
    case '^':
        return 4;
    case '(':
    case ')':
        return 1;
    default:
        return 0;
    }
}

std::string _calculate(std::string expression) {
    std::stack<char> operate; // 存放运算符
    std::string suffix;       // 后缀表达式

    bool lastIsOp = false;
    size_t cur = 0;
    while (cur < expression.length()) {
        if (expression[cur] >= '0' && expression[cur] <= '9') { // 数字
            suffix.push_back('|');
            while (expression[cur] >= '0' && expression[cur] <= '9') { // 整数部分
                suffix.push_back(expression[cur++]);
            }
            if (expression[cur] == '.') {
                suffix.push_back(expression[cur++]);
                while (expression[cur] >= '0' && expression[cur] <= '9') //小数部分
                {
                    suffix.push_back(expression[cur++]);
                }
            }
            suffix.push_back('|');
            lastIsOp = false;
        } else if (expression[cur] == '(') { // 左括号 优先级最高，放入栈中1
            operate.push(expression[cur++]);
            lastIsOp = true;
        } else if (expression[cur] ==
                   ')') { // 右括号，将栈内运算符取出放入输出字符串，直到取出左括号
            cur++;
            if (operate.empty()) {
                return "";
            } else {
                while (!operate.empty() && operate.top() != '(') {
                    suffix.push_back(operate.top());
                    operate.pop();
                }
                if (operate.empty()) {
                    return "";
                } else {
                    operate.pop();
                }
            }
            lastIsOp = true;
        } else if (expression[cur] == '+' || expression[cur] == '-' || expression[cur] == '*' ||
                   expression[cur] == '/' || expression[cur] == '^') { // 运算符

            if (expression[cur] == '-') { // 负数
                if (cur == 0 || lastIsOp) {
                    suffix.push_back('|');
                    suffix.push_back('-');
                    cur++;
                    while (cur < expression.length() &&
                           (expression[cur] == ' ' || expression[cur] == '\t' ||
                            expression[cur] == '\n')) {
                        cur++;
                    }
                    if (expression[cur] >= '0' && expression[cur] <= '9') // 数字
                    {
                        while (expression[cur] >= '0' && expression[cur] <= '9') { // 整数部分
                            suffix.push_back(expression[cur++]);
                        }
                        if (expression[cur] == '.') {
                            suffix.push_back(expression[cur++]);
                            while (expression[cur] >= '0' && expression[cur] <= '9') //小数部分
                            {
                                suffix.push_back(expression[cur++]);
                            }
                        }
                    }
                    suffix.push_back('|');
                    continue;
                }
            }

            //如果读入一般运算符如+-*/，则放入堆栈，但是放入堆栈之前必须要检查栈顶
            if (operate.empty()) {
                operate.push(expression[cur++]);
            } else {
                char top = operate.top();                            // 栈顶
                if (getPriority(top) < getPriority(expression[cur])) // 符号优先级低于栈顶
                {
                    operate.push(expression[cur++]); //放入栈顶并指向下一个
                } else // 否则，将栈顶的运算符放入输出字符串。
                {
                    while (getPriority(top) >= getPriority(expression[cur])) {
                        suffix.push_back(top);
                        operate.pop();
                        if (!operate.empty()) {
                            top = operate.top();
                        } else {
                            break;
                        }
                    }
                    operate.push(expression[cur++]); //放入栈顶并指向下一个
                }
            }
            lastIsOp = true;
        } else if (expression[cur] == ' ' || expression[cur] == '\t' || expression[cur] == '\n') {
            cur += 1;
            continue;
        } else {
            return "";
        }
    }
    //顺序读完表达式，如果栈中还有操作符，则弹出，并放入输出字符串。
    while (!operate.empty()) {
        suffix.push_back(operate.top());
        operate.pop();
    }

    // cout << "后缀为：" << suffix << endl;

    std::stack<double> number;
    cur = 0;
    std::string current;
    while (cur < suffix.length()) {
        if (suffix[cur] == '|') { // 数字
            cur++;
            int offset = cur;
            while (suffix[cur] != '|') {
                cur++;
            }
            current = suffix.substr(offset, cur - offset);
            number.push(std::stod(current));
        } else {
            if (number.size() < 2) {
                return "";
            }

            current = suffix.substr(cur, 1);
            double n2 = number.top();
            number.pop();
            double n1 = number.top();
            number.pop();
            if (current == "+") {
                number.push(n1 + n2);
            } else if (current == "-") {
                number.push(n1 - n2);
            } else if (current == "*") {
                number.push(n1 * n2);
            } else if (current == "/") {
                number.push(n1 / n2);
            } else if (current == "^") {
                number.push(std::pow(n1, n2));
            }
        }
        cur++;
    }
    if (number.size() != 1) {
        return "";
    } else {
        return std::to_string(number.top());
    }
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
