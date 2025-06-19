using Discord;
using Discord.Interactions;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// Modal used for editing a scheduled announcement's content.
    /// </summary>
    public class EditAnnouncementModal : IModal
    {
        /// <summary>
        /// Title of the modal displayed to the user.
        /// </summary>
        public string Title => "Edit Announcement";

        /// <summary>
        /// The title of the embed to be edited.
        /// </summary>
        [InputLabel("Title")]
        [ModalTextInput("editAnnouncementModal_embedTitle", TextInputStyle.Paragraph, "ff", maxLength: 256)]
        public string EmbedTitle { get; set; }

        /// <summary>
        /// The description of the embed to be edited.
        /// </summary>
        [InputLabel("Description")]
        [ModalTextInput("editAnnouncementModal_description", TextInputStyle.Paragraph, "ff", maxLength: 4000)]
        public string Description { get; set; }
    }
}