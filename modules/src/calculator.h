#ifndef __MODULES__CALCULATOR_H__
#define __MODULES__CALCULATOR_H__

#include <cmath>
#include <stack>

#include "base.h"

class Calculator : public Base {
public:
    std::string query(std::string expression) {
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
            } else if (expression[cur] == ' ' || expression[cur] == '\t' ||
                       expression[cur] == '\n') {
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
                size_t offset = cur;
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
            std::string ans = std::to_string(number.top());
            for (char c : ans) {
                if (c == '.') {
                    while (ans.back() == '0') {
                        ans.pop_back();
                    }
                    if (ans.back() == '.') {
                        ans.pop_back();
                    }
                    break;
                }
            }
            return ans;
        }
    }

private:
    inline int getPriority(char operate) // 栈内优先级
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
};

#endif // __MODULES__CALCULATOR_H__
