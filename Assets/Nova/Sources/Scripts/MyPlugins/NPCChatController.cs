using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NPCChatController : MonoBehaviour
    {
        public Text text;
        public InputField inputField;
        public Button closeButton;
        public Button submitButton;

        private GameState gameState;
        private Variables variables;
        private CheckpointHelper checkpointHelper;
        private GPTClient GPT;

        private string name;
        private string background;
        private string bye;

        private void Awake()
        {
            GPT = new GPTClient();
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            variables = gameState.variables;
            checkpointHelper = controller.CheckpointHelper;

            background = variables.Get<string>("v_background");
            name = variables.Get<string>("v_name");
            bye = variables.Get<string>("v_bye");

            variables.Set<bool>("v_flag_fin", false);

            closeButton.onClick.AddListener(OnClickCancel);
            submitButton.onClick.AddListener(OnClickSubmit);

            // 调试输出：初始化完成
            Debug.Log("ExampleMinigameController initialized.");
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(OnClickCancel);
            submitButton.onClick.RemoveListener(OnClickSubmit);
        }

        private void OnClickCancel()
        {
            // 取消按钮被点击
            Debug.Log("Cancel button clicked.");
            variables.Set<bool>("v_flag_fin", true);
            variables.Set("v_response", bye);
            checkpointHelper.SetGlobalVariable<int>("gv_flag", 0);

            gameState.SignalFence(true);
        }

        private void OnClickSubmit()
        {
            // 提交按钮被点击
            Debug.Log("Submit button clicked.");

            var userInput = inputField.text;
            // 输出 NPC 信息和用户输入
            Debug.Log($"NPC Name: {name}");
            Debug.Log($"NPC Background: {background}");
            Debug.Log($"User Input: {userInput}");
            string job = checkpointHelper.GetGlobalVariable<string>("gv_job");
            string reveal = variables.Get<string>("v_reveal");
            string unreveal = variables.Get<string>("v_unreveal");
            string personality = variables.Get<string>("v_personality");

            int flag = checkpointHelper.GetGlobalVariable<int>("gv_flag");
            Debug.Log("gv:{0}"+flag);
            // 开始协程，调用 GPT API
            StartCoroutine(GPT.GetGptResponse(name, background, personality,reveal,unreveal,job,flag, userInput, onGPTResponse));

            // 确认协程被启动
            Debug.Log("Started GetGptResponse coroutine.");
        }

        private void onGPTResponse(string response)
        {
            Debug.Log("callback");
            if (!response.Equals("对不起，我现在无法回答您的问题。"))
            {
                variables.Set("v_response", response);
            }
            else
            {
                checkpointHelper.SetGlobalVariable<int>("gv_flag", 1);
                variables.Set("v_response", variables.Get<string>("v_fail"));
            }
            // 把 GPT 响应设置到变量中
            gameState.SignalFence(true);
        }
    }
}
