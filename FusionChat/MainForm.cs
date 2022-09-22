using FusionChat.Service;
using Stl.Fusion;

namespace FusionChat
{
    public partial class MainForm : Form
    {
        private IChatService _chatService = default!;
        private IComputedState<ChatInfo> _computedState = default!;

        private int messageIndex = 0;

        public MainForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            inputText.Focus();


            var client = new FusionChatClient(new Uri("https://localhost:7233/"));
            _computedState = client.StateFactory.NewComputed<ChatInfo>(new ComputedState<ChatInfo>.Options()
            {
                UpdateDelayer = FixedDelayer.Instant
            }, async (state, CancellationToken) =>
            {
                var result = await client.ChatService.GetChatMessages(messageIndex);
                messageIndex = result.TotalMessages;

                this.BeginInvoke(() =>
                {
                    foreach (var message in result.Messages)
                    {
                        chatText.AppendText($"{message.Nickname}: {message.Message}\r\n");
                    }
                });

                return result;
            });

            _chatService = client.ChatService;
        }

        private async void sendButton_Click(object sender, EventArgs e)
        {
            await _chatService.SendMessage(new ChatMessage(nicknameText.Text, inputText.Text));

            inputText.Text = "";
        }

        private void inputText_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode is Keys.Enter)
            {
                sendButton.PerformClick();
                return;
            }
        }
    }
}