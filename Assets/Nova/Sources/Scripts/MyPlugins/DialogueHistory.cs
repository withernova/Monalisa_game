using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class DialogueHistory : Singleton<DialogueHistory>
{
    public Dictionary<string, List<Message>> conversationHistory = new Dictionary<string, List<Message>>();

    // 添加用户的消息
    public void AddUserMessage(string name, string userMessage)
    {
        if (!conversationHistory.ContainsKey(name))
        {
            conversationHistory.Add(name, new List<Message>());
        }
        conversationHistory[name].Add(new Message { role = "user", content = userMessage });
    }

    // 添加 GPT 的回复
    public void AddAssistantMessage(string name, string assistantMessage)
    {
        if (!conversationHistory.ContainsKey(name))
        {
            conversationHistory.Add(name, new List<Message>());
        }
        conversationHistory[name].Add(new Message { role = "assistant", content = assistantMessage });
    }

    public void AddSystemMessage(string name, string systemMessage)
    {
        if (!conversationHistory.ContainsKey(name))
        {
            conversationHistory.Add(name, new List<Message>());
        }
        conversationHistory[name].Add(new Message { role = "system", content = systemMessage });
    }
}
