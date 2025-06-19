using Discord;
using Discord.Interactions;

namespace Bob.Feedback.Models
{
    /// <summary>
    /// Modal used for suggesting a unit to the user.
    /// </summary>
    public class SuggestUnitModal : IModal
    {
        /// <summary>
        /// Title of the modal displayed to the user.
        /// </summary>
        public string Title => "Suggest a Unit to Add to Conversions";

        /// <summary>
        /// The content of the message to be edited.
        /// </summary>
        [InputLabel("Message Content")]
        [ModalTextInput("suggestUnitModal_content", TextInputStyle.Paragraph, "ff", maxLength: 1500)]
        public string Content { get; set; }
    }
}