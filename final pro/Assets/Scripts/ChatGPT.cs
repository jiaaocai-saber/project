using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using static UnityEditor.FilePathAttribute;

namespace OpenAI
{
    public class ChatGPT : MonoBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button button;
        [SerializeField] private ScrollRect scroll;
        
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;

        private float height;
        private OpenAIApi openai = new OpenAIApi();

        private List<ChatMessage> messages = new List<ChatMessage>();

        [TextArea] // 让prompt在编辑器中显示为多行文本框
        public string prompt;
        //private string prompt = "You are Serissa, the youngest daughter of King Hekaton and Queen Neri, from the Dungeons & Dragons adventure 'Storm King's Thunder'. You are a storm giant and currently preside over your father's court in Maelstrom, deep within the Trackless Sea. You seek to uncover the truth about your mother's death and your father's disappearance, all while holding the storm giant court together. You are intelligent, wise, and fair, but troubled by recent events. Players who encounter you need to earn your trust and provide information about your father and the events surrounding your family's downfall. Respond with caution, but be open to listening and seeking the truth. If players ask something outside the context of this world, respond with, 'Sorry, I don't know what you are talking about.";

        public EntityCreator entityCreator; // 引用 EntityCreator 脚本

        public UnityEvent OnReply;

        private async void Start()
        {
            button.onClick.AddListener(SendReply);
            // 上传PDF文件
            var uploadResult = await UploadInitialPdfFiles();
            if (uploadResult)
            {
                Debug.Log("PDF files uploaded successfully.");
            }
            else
            {
                Debug.LogError("Failed to upload PDF files.");
            }
        }
        private async Task<bool> UploadInitialPdfFiles()
        {
            // 定义PDF文件路径
            string pdfFilePath1 = Application.dataPath + "/File/Doc1.pdf";
            string pdfFilePath2 = Application.dataPath + "/File/Doc2.pdf";

            // 上传第一个PDF文件
            var upload1 = await UploadFile(pdfFilePath1, "user_data");

            // 上传第二个PDF文件
            var upload2 = await UploadFile(pdfFilePath2, "user_data");

            // 确保两个文件都上传成功
            return upload1 != null && upload2 != null;
        }

        private async Task<OpenAIFile> UploadFile(string filePath, string purpose)
        {
            var request = new CreateFileRequest
            {
                File = filePath,
                Purpose = purpose
            };

            return await openai.CreateFile(request);
        }



        private void AppendMessage(ChatMessage message)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            var item = Instantiate(message.Role == "user" ? sent : received, scroll.content);
            // 设置消息文本
            item.GetChild(0).GetChild(0).GetComponent<Text>().text = message.Content;
            // 强制刷新布局以获得正确的高度
            item.anchoredPosition = new Vector2(0, -height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            // 更新scroll content的高度
            height += item.sizeDelta.y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
        }

        private async void SendReply()
        {
            var newMessage = new ChatMessage()
            {
                Role = "user",
                Content = inputField.text
            };
            
            AppendMessage(newMessage);

            if (messages.Count == 0) newMessage.Content = prompt + "\n" + inputField.text; 
            
            messages.Add(newMessage);
            
            button.enabled = false;
            inputField.text = "";
            inputField.enabled = false;
            
            // Complete the instruction
            var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                //Model = "gpt-4o",
                Model = "gpt-3.5-turbo",
                Messages = messages
            });

            if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
            {
                var message = completionResponse.Choices[0].Message;
                message.Content = message.Content.Trim();
                
                messages.Add(message);
                AppendMessage(message);

                // 检查是否包含特殊格式的指令
                CheckForEntityCreationCommand(message.Content);
                Debug.Log($"do check{message.Content}");
            }
            else
            {
                Debug.LogWarning("No text was generated from this prompt.");
            }

            OnReply.Invoke();

            button.enabled = true;
            inputField.enabled = true;
        }

        private void CheckForEntityCreationCommand(string response)
        {
            // 修改正则表达式来匹配普通的文本
            var match = Regex.Match(response, @"(?:monster|thing)\s+(?:in the room|behind the wall)");

            if (match.Success && entityCreator != null)
            {
                // 从匹配结果中提取实体类型和位置
                string entityType = match.Value.Contains("monster") ? "monster" : "thing";
                string location = match.Value.Contains("the room") ? "in the room" : "behind the wall";


                Debug.Log($"Entity creation command detected: Type = {entityType}, Location = {location}");

                entityCreator.CreateEntity(entityType, location);
            }
        }
    }
}
