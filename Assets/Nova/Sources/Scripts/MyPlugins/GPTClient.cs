using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class Message
{
    public string role;
    public string content;
}

public class GPTClient
{
    // OpenAI API 密钥
    private string apiKey = "hk-i5lxzq10000439219559940233309a39ac00c7025c64f879";

    // 构建 Prompt 的函数
    private string BuildPrompt(string npcName, string npcBackground, string npcPersonality, string canRevealDirectMessages, string canRevealIndirectMessages, string job, int flag, string userMessage)
    {
        string prompt = string.Format(
            $"玩家的职业是 {job}。" +
            $"而你的名字是 {npcName}，你在这个游戏中扮演的角色背景background是：{npcBackground}。" +
            $"你的人物性格personality是：{npcPersonality}。" +
            $"当玩家问到该消息相关的点子（如果下面的内容有‘关于’二字，那么必须要问到相关的内容再说出来）上你便可以直接透露的reveal信息有（可以按分号一次只给一个或两个）：{canRevealDirectMessages}。" +
            $"只有当玩家问到该消息相关的点子并且玩家要进行职业的roleplay你才会给他们透露的unreveal消息为：{canRevealIndirectMessages}。" +
            $"现在玩家前来问你：{userMessage}。"
        );
        return prompt;
    }

    private string BuildPrompt(string answer,string input)
    {
        string prompt = string.Format(
            $"你已知的潜在正确答案为:{answer}。" +
            $"玩家的比较对象为{input}。"
            );
        return prompt;
    }

    // 定义请求体类
    private class RequestBody
    {
        public string model;
        public Message[] messages;
        public float temperature;
        public int max_tokens;
        public float top_p;
        public float presence_penalty;
    }

    // 定义响应类
    [Serializable]
    public class GptResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public MessageDetail message;
    }

    [Serializable]
    public class MessageDetail
    {
        public string role;
        public string content;
    }

    // 发送 HTTP POST 请求的协程函数
    public IEnumerator GetGptResponse(
        string npcName,
        string npcBackground,
        string npcPersonality,
        string canRevealDirectMessages,
        string canRevealIndirectMessages,
        string job,
        int flag,
        string userMessage,
        Action<string> callback)
    {
        // 添加用户的输入到历史记录中
        if (!DialogueHistory.instance.conversationHistory.ContainsKey(npcName))
        {
            DialogueHistory.instance.AddSystemMessage(npcName, "  假设你是一个跑团中的NPC，游戏背景是世界上突然出现了一个叫张三的人，他能从分子结构上完全仿造出一幅蒙娜丽莎的画作。根据你的设定，进行合理的对话，只需回答玩家当前的问题，不包含历史对话记录，但可参考之前的回答以避免重复信息。\n\n你拥有以下信息：\n\njob：玩家的职业\nname：你的名字\nbackground：你的角色背景，可适当提及，但不宜过多\npersonality：你的性格特征，应在对话中体现\nreveal：可通过简单询问透露的信息，可以根据你的reveal信息中的分号来分别根据前置的玩家询问要求来给出信息”\nunreveal：不易透露的信息，只有在玩家尝试角色扮演时才可能透露，并且是逐步揭示\n注意：\n\n只有当玩家问到已知信息时，才会透露可直接透露的背景信息。\n玩家进行职业角色扮演时，根据其方式揭示unreveal信息。\n其他情况下，可给出模糊提示或不明确的否认。\n玩家选择的职业有四种：高魅力（charm）、高技术（tech）、高力量和威慑力（barbarian）、高神秘和魔法（magician）。例如，charm尝试取悦你时，你可以分享信息；barbarian威胁你时，你可能被迫透露信息；magician用神秘话术时，你可以给予信息；tech用道理说服你时，可以适当分享信息。\n\n你是一个跑团游戏中的NPC，回答时要体现角色性格，尽量用“你”来称呼玩家，不要未卜先知就以玩家的职业称呼玩家。可以在对话开始时根据性格和背景说些创造性的开头，不要一次给出所有细节。回答时自然流畅，避免脱离游戏沉浸感。注意，不要有多余标点，直接以第一人称视角回答，就像在回答一个人问的问题一样，自然对话。在首次询问时，会补充关于你扮演角色的更多细节，请牢记这些信息。");
        userMessage = BuildPrompt(npcName, npcBackground, npcPersonality, canRevealDirectMessages, canRevealIndirectMessages, job, flag, userMessage);
        }
        DialogueHistory.instance.AddUserMessage(npcName, userMessage);

        // 构建请求体
        RequestBody requestBody = new RequestBody()
        {
            model = "gpt-3.5-turbo",
            messages = DialogueHistory.instance.conversationHistory[npcName].ToArray(), // 将对话历史记录传递给 GPT
            temperature = 0.8f,
            max_tokens = 1200,
            top_p = 1.0f,
            presence_penalty = 1.0f
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest("https://api.openai-hk.com/v1/chat/completions", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // 禁用重定向
            request.redirectLimit = 0;

            // 设置超时时间
            request.timeout = 30;

            // 预设值：当请求失败时使用的默认响应
            string fallbackResponse = "对不起，我现在无法回答您的问题。";

            // 发送请求并等待响应
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.Success)
#else
            if (!request.isNetworkError && !request.isHttpError)
#endif
            {
                try
                {
                    GptResponse gptResponse = JsonConvert.DeserializeObject<GptResponse>(request.downloadHandler.text);
                    if (gptResponse != null && gptResponse.choices.Length > 0)
                    {
                        string responseContent = gptResponse.choices[0].message.content;

                        // 添加 GPT 的回复到历史记录中
                        DialogueHistory.instance.AddAssistantMessage(npcName, responseContent);

                        // 返回 GPT 的回复
                        callback?.Invoke(responseContent);
                        Debug.Log(responseContent);
                    }
                    else
                    {
                        // 如果响应格式不正确，调用预设值
                        callback?.Invoke(fallbackResponse);
                    }
                }
                catch (Exception ex)
                {
                    // 如果出现解析错误，调用预设值
                    callback?.Invoke(fallbackResponse);
                }
            }
            else
            {
                // 如果 HTTP 请求失败，调用预设值
                string errorMsg = "HTTP 请求失败，状态码: " + request.responseCode + " 错误信息: " + request.error;
                callback?.Invoke(fallbackResponse);
            }
        }
    }


    public IEnumerator GetGptResponse(
       string id,
       string require,
       string userInput,
       Action<string> callback)
    {
        string userMessage = "";
        // 添加用户的输入到历史记录中
        if (!DialogueHistory.instance.conversationHistory.ContainsKey(id))
        {
            DialogueHistory.instance.AddSystemMessage(id, "你是一个能够识别两者意义是否相似的机器人。我在进行一个类似猜谜小游戏，猜谜小游戏中玩家会给出一个回答，你要对比玩家给出的回答是否和正确答案指的是相似的物品或者概念，如果相似的话请务必直接返回一个数字1，否则直接返回一个数字0");
            userMessage = BuildPrompt(require,userInput);
        }
        DialogueHistory.instance.AddUserMessage(id, userMessage);

        // 构建请求体
        RequestBody requestBody = new RequestBody()
        {
            model = "gpt-3.5-turbo",
            messages = DialogueHistory.instance.conversationHistory[id].ToArray(), // 将对话历史记录传递给 GPT
            temperature = 0.5f,
            max_tokens = 1200,
            top_p = 1.0f,
            presence_penalty = 1.0f
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest("https://api.openai-hk.com/v1/chat/completions", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // 禁用重定向
            request.redirectLimit = 0;

            // 设置超时时间
            request.timeout = 30;

            // 预设值：当请求失败时使用的默认响应
            string fallbackResponse = "对不起，我现在无法回答您的问题。";

            // 发送请求并等待响应
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.Success)
#else
            if (!request.isNetworkError && !request.isHttpError)
#endif
            {
                try
                {
                    GptResponse gptResponse = JsonConvert.DeserializeObject<GptResponse>(request.downloadHandler.text);
                    if (gptResponse != null && gptResponse.choices.Length > 0)
                    {
                        string responseContent = gptResponse.choices[0].message.content;

                        // 添加 GPT 的回复到历史记录中
                        DialogueHistory.instance.AddAssistantMessage(id, responseContent);

                        // 返回 GPT 的回复
                        callback?.Invoke(responseContent);
                        Debug.Log(responseContent);
                    }
                    else
                    {
                        // 如果响应格式不正确，调用预设值
                        callback?.Invoke(fallbackResponse);
                    }
                }
                catch (Exception ex)
                {
                    // 如果出现解析错误，调用预设值
                    callback?.Invoke(fallbackResponse);
                }
            }
            else
            {
                // 如果 HTTP 请求失败，调用预设值
                string errorMsg = "HTTP 请求失败，状态码: " + request.responseCode + " 错误信息: " + request.error;
                callback?.Invoke(fallbackResponse);
            }
        }
    }
}
