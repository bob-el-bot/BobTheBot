using Discord;
using Discord.Interactions;

namespace Commands.Helpers
{
    /// <summary>
    /// Modal used for editing a scheduled message's content.
    /// </summary>
    public class EditMessageModal : IModal
    {
        /// <summary>
        /// Title of the modal displayed to the user.
        /// </summary>
        public string Title => "Edit Message";

        /// <summary>
        /// The content of the message to be edited.
        /// </summary>
        [InputLabel("Message Content")]
        [ModalTextInput("editMessageModal_content", TextInputStyle.Paragraph, "ff", maxLength: 2000)]
        public string Content { get; set; }
    }
}