#ifndef __MODULES__LRU_CACHE_H__
#define __MODULES__LRU_CACHE_H__

#include <map>

template <typename K, typename V>
class LRUCache {
private:
    struct LinkedNode {
        K key;
        V val;
        LinkedNode* pre;
        LinkedNode* next;
        LinkedNode() : pre(NULL), next(NULL) {}
        LinkedNode(K key, V val) : key(key), val(val), pre(NULL), next(NULL) {}
    };

    LinkedNode* head;
    LinkedNode* tail;
    int capacity, size;
    std::map<K, LinkedNode*> container;

    void mov2head(LinkedNode* node) {
        node->next->pre = node->pre;
        node->pre->next = node->next;
        this->head->next->pre = node;
        node->next = this->head->next;
        this->head->next = node;
        node->pre = this->head;
    }

public:
    LRUCache(int capacity) {
        this->capacity = capacity;
        this->size = 0;
        this->head = new LinkedNode();
        this->tail = new LinkedNode();
        this->head->next = this->tail;
        this->tail->pre = this->head;
        std::cout << "create" << std::endl;
    }
    ~LRUCache() { std::cout << "destory" << std::endl; }

    bool get(const K& key, V& value) {
        if (this->container.find(key) == this->container.end()) {
            return false;
        } else {
            LinkedNode* node = this->container[key];
            mov2head(node);
            value = node->val;
            return true;
        }
    }

    bool put(const K& key, const V& value) {
        if (this->container.find(key) != this->container.end()) {
            LinkedNode* node = this->container[key];
            node->val = value;
            mov2head(node);
        } else {
            if (this->size == this->capacity) {
                LinkedNode* node = this->tail->pre;
                this->container.erase(node->key);
                node->key = key;
                node->val = value;
                this->container[key] = node;
                mov2head(node);
            } else {
                this->size++;
                LinkedNode* node = new LinkedNode(key, value);
                this->container[key] = node;
                node->next = this->head->next;
                node->pre = this->head;
                this->head->next->pre = node;
                this->head->next = node;
            }
        }
        return true;
    }
};

#endif // !__MODULES__LRU_CACHE_H__
