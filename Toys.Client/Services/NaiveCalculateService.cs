using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class NaiveCalculateService : ICalculateService
    {
        private bool Enable { get; set; }
        private readonly Regex inputValidator = new Regex("^([0-9]|[x\\+\\-\\/\\(\\)\\*\\^\\.\\s])*$");

        public NaiveCalculateService(CalculateSetting setting)
        {
            Enable = setting.Enable;
        }

        public List<CalculateEntry> Calculate(string question)
        {
            List<CalculateEntry> ans = new List<CalculateEntry>();
            if (!Enable) return ans;

            question = Preprocess(question);
            if (question != string.Empty)
            {
                try
                {
                    question = ConvertInfixToPostfix(question);
                    ans.Add(new CalculateEntry(EvaluatePostfixExpression(question).ToString()));
                }
                catch (Exception)
                {
                }
            }
            return ans;
        }

        private string Preprocess(string input)
        {
            // preprocess
            if (input[0] == '.')
            {
                input = "0" + input;
            }
            input = input.Replace('（', '(').Replace('）', ')');
            input = input.Replace('、', '/').Replace('《', '<');
            input = input.Replace("**", "^").Replace("<<", "*2^");

            // match regex
            if (!inputValidator.IsMatch(input)) return string.Empty;

            // match parens
            ArrayStack<char> parensStack = new ArrayStack<char>();
            foreach (char c in input)
            {
                if (c.CompareTo('(') == 0)
                {
                    parensStack.Push(c);
                }
                else if (c.CompareTo(')') == 0)
                {
                    if (parensStack.IsEmpty() || parensStack.Pop().CompareTo('(') != 0)
                    {
                        return string.Empty;
                    }
                }
            }
            if (!parensStack.IsEmpty())
            {
                return string.Empty;
            }

            // return string of space-seperated values
            StringBuilder stringy = new StringBuilder(); //used for constructing the return value
            string operators = "+-/*x^()"; //list of non-numeric characters. Period counts as numeric
            char[] chars = input.ToCharArray();
            bool processingNumber = false; //are we currently processing a number?
            for (int i = 0; i < chars.Length; i++)
            {
                if (operators.Contains(chars[i])) //we have an operator
                {
                    if (processingNumber) //we were processing a number but are not anymore
                    {
                        stringy.Append(" ");
                        processingNumber = false;
                    }
                    stringy.Append(chars[i]);
                    stringy.Append(" ");
                }
                else if (chars[i].Equals("") || chars[i].Equals(' ') || chars[i].Equals('\t')) //we have whitespace or tab
                {
                    continue;
                }
                else //we have a number or period
                {
                    stringy.Append(chars[i]);
                    processingNumber = true;
                }
            }
            return stringy.ToString().Trim();
        }

        private string ConvertInfixToPostfix(string input)
        {
            string[] values = input.Split(' ');
            StringBuilder rpn = new StringBuilder(input.Length);
            ArrayDictionary<string, int> operators = new ArrayDictionary<string, int>(5);
            operators.Add("^", 4);
            operators.Add("*", 3);
            operators.Add("x", 3);
            operators.Add("/", 3);
            operators.Add("+", 2);
            operators.Add("-", 2);
            operators.Add("(", -1);
            operators.Add(")", -1);
            ArrayStack<string> operatorStack = new ArrayStack<string>();

            foreach (string item in values)
            {
                if (item.Equals(""))
                {
                    break;
                }
                if (operators.ContainsKey(item)) //item is an operator
                {
                    if (operatorStack.Size == 0) //stack is empty, we push operator to stack
                    {
                        operatorStack.Push(item);
                        continue;
                    }

                    if (item.Equals(")")) //we have a right paren
                    {
                        bool found = false;
                        while (!operatorStack.IsEmpty()) //searching for left paren
                        {
                            if (operatorStack.Peek().Equals("("))
                            {
                                found = true;
                                operatorStack.Pop();
                                break;
                            }
                            else
                            {
                                rpn.Append(operatorStack.Pop());
                                rpn.Append(" ");
                            }
                        }
                        if (!found && operatorStack.IsEmpty()) //stack empty, no left paren in sight
                        {
                            throw new ArgumentException("Mismatched parens");
                        }
                    }
                    else if (item.Equals("(")) //left parens always added to stack
                    {
                        operatorStack.Push(item);
                    }
                    else //we have an operator
                    {
                        if (item.Equals("^") && operators[item] < operators[operatorStack.Peek()])
                        {
                            rpn.Append(operatorStack.Pop());
                            rpn.Append(" ");
                        }
                        else if (operators[item] <= operators[operatorStack.Peek()])
                        {
                            rpn.Append(operatorStack.Pop());
                            rpn.Append(" ");
                        }
                        operatorStack.Push(item);
                    }
                }
                else //item is a number
                {
                    double n;
                    if (!double.TryParse(item, out n))
                        throw new FormatException("Invalid input of number");
                    rpn.Append(n);
                    rpn.Append(" ");
                }
            }
            while (!operatorStack.IsEmpty()) //we're done, dump stack onto output
            {
                if (operatorStack.Peek().Equals(")") || operatorStack.Peek().Equals("("))
                    throw new ArgumentException("Mismatched parens");
                rpn.Append(operatorStack.Pop());
                rpn.Append(" ");
            }

            return rpn.ToString().Trim();
        }

        private double EvaluatePostfixExpression(string rpn)
        {
            string[] values = rpn.Split(' ');
            ArrayStack<object> calculationStack = new ArrayStack<object>();
            foreach (string s in values)
            {
                if (double.TryParse(s, out double n))
                {
                    calculationStack.Push(n);
                }
                else //if (leftOperators.Contains(s))
                {
                    try
                    {
                        double right = double.Parse(calculationStack.Pop().ToString());
                        double left = double.Parse(calculationStack.Pop().ToString());

                        switch (s)
                        {
                            case "+":
                                calculationStack.Push((left + right).ToString());
                                break;

                            case "-":
                                calculationStack.Push((left - right).ToString());
                                break;

                            case "/":
                                calculationStack.Push((left / right).ToString());
                                break;

                            case "*":
                                calculationStack.Push((left * right).ToString());
                                break;

                            case "x":
                                calculationStack.Push((left * right).ToString());
                                break;

                            case "^":
                                calculationStack.Push((Math.Pow(left, right)).ToString());
                                break;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new FormatException("Invalid format. Please avoid double negation (--5) and rewrite negative numbers as 0 - n.");
                    }
                }
            }
            return double.Parse(calculationStack.Pop().ToString());
        }
    }

    class ArrayStack<T>
    {
        private T[] stack;
        private int size;

        public ArrayStack() : this(10)
        {
        }

        public ArrayStack(int capacity)
        {
            stack = new T[capacity];
        }

        public void Push(T item)
        {
            if (null == item)
            {
                Console.WriteLine("Attempted to add null to stack");
            }
            if (size >= stack.Length - 1)
            {
                T[] temp = new T[stack.Length * 2];
                for (int i = 0; i < stack.Length; i++)
                {
                    temp[i] = stack[i];
                }
                stack = temp;
            }
            stack[size] = item;
            size++;
        }

        public T Pop()
        {
            if (size == 0)
            {
            }
            size--;
            T ret = stack[size];
            stack[size] = default(T);
            return ret;
        }

        public T Peek()
        {
            T ret = Pop();
            Push(ret);
            return ret;
        }

        public bool IsEmpty()
        {
            return size == 0;
        }

        public void Clear()
        {
            size = 0;
            stack = new T[stack.Length];
        }

        public int Size
        {
            get { return size; }
        }

        public override string ToString()
        {
            StringBuilder stringy = new StringBuilder();
            for (int i = size; i >= 0; i--)
            {
                stringy.Append(stack[i]);
                stringy.Append(" ");
            }
            return stringy.ToString();
        }
    }

    class ArrayDictionary<T, E>
    {
        private T[] keys;
        private E[] values;
        private int size;

        public ArrayDictionary() : this(10)
        {
        }

        public ArrayDictionary(int capacity)
        {
            keys = new T[capacity];
            values = new E[capacity];
        }

        public void Add(T key, E value)
        {
            if (null == value)
                throw new ArgumentNullException();
            if (size >= keys.Length - 1)
            {
                T[] tempKeys = new T[keys.Length * 2];
                E[] tempValues = new E[values.Length * 2];
                for (int i = 0; i < keys.Length; i++)
                {
                    tempKeys[i] = keys[i];
                    tempValues[i] = values[i];
                }
                keys = tempKeys;
                values = tempValues;
            }
            keys[size] = key;
            values[size] = value;
            size++;
        }

        public bool Remove(T key)
        {
            if (key == null)
                throw new ArgumentNullException();
            for (int i = 0; i < size; i++)
            {
                if (keys[i].Equals(key))
                {
                    for (int j = i + 1; j < size; j++)
                    {
                        keys[j - 1] = keys[j];
                    }
                    break;
                }
            }
            return false;
        }

        public bool ContainsKey(T key)
        {
            if (key == null)
                throw new ArgumentNullException();
            foreach (T item in keys)
            {
                if (item == null)
                    return false;
                if (item.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsValue(E value)
        {
            if (value == null)
                throw new ArgumentNullException();
            foreach (E item in values)
            {
                if (item.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsEmpty()
        {
            return size == 0;
        }

        public void Clear()
        {
            size = 0;
            keys = new T[keys.Length];
            values = new E[keys.Length];
        }

        public int Size
        {
            get { return size; }
        }

        public E this[T key]
        {
            get
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (key.Equals(keys[i]))
                        return values[i];
                }
                throw new KeyNotFoundException();
            }
            set
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (key.Equals(keys[i]))
                        values[i] = value;
                }
                throw new KeyNotFoundException();
            }
        }
    }
}
