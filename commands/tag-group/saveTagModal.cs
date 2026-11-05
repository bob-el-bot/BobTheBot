using Discord;
using Discord.Interactions;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// Modal used for saving a message as a tag.
    /// </summary>
    public class SaveTagModal : IModal
    {
        /// <summary>
        /// Title of the modal displayed to the user.
        /// </summary>
        public string Title => "Save as Tag";

        /// <summary>
        /// The name of the new tag.
        /// </summary>
        [InputLabel("Tag Name")]
        [ModalTextInput("saveTagModal_name", TextInputStyle.Short, "e.g. rules", minLength: 1, maxLength: 50)]
        public string Name { get; set; }

        /// <summary>
        /// The content of the new tag, prefilled with the message's content.
        /// </summary>
        [InputLabel("Tag Content")]
        [ModalTextInput("saveTagModal_content", TextInputStyle.Paragraph, minLength: 5, maxLength: 2000)]
        public string Content { get; set; }
    }
}
