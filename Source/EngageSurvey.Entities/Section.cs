﻿// <copyright file="Section.cs" company="Engage Software">
// Engage: Survey
// Copyright (c) 2004-2015
// by Engage Software ( http://www.engagesoftware.com )
// </copyright>
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

namespace Engage.Survey.Entities
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;

    using Engage.Survey.UI;

    using Util;

    /// <summary>
    /// Section class of a Survey
    /// </summary>
    public partial class Section : ISection
    {
        /// <summary>
        /// Gets the formatting for the element plus the the unformatted text together. Used primarily by
        /// the Web and Windows viewers only.
        /// </summary>
        /// <value></value>
        public string FormattedText
        {
            get { return this.ShowText ? (this.Formatting + this.UnformattedText) : string.Empty; }
        }

        /// <summary>
        /// Gets only the text value of Text attribute for the survey element.
        /// </summary>
        /// <value></value>
        public string UnformattedText
        {
            get { return this.Text; }
        }
        
        /// <summary>
        /// Gets the formatting that will be used to prefix the unformatted text for the survey element.
        /// </summary>
        /// <value></value>
        public string Formatting
        {
            get
            {
                ISurvey survey = this.Survey;
                if (survey != null)
                {
                    return Utility.PrependFormatting(survey.SectionFormatOption, this.RelativeOrder);
                }

                return string.Empty;
            }
        }

        #region ISection signatures

        /// <summary>
        /// Gets the question.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns>An IQuestion using the passed key.</returns>
        public IQuestion GetQuestion(Key key)
        {
            foreach (IQuestion question in this.GetQuestions())
            {
                if (question.QuestionId == key.QuestionId)
                {
                    return question;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the survey.
        /// </summary>
        /// <returns>The reference to the survey (owner).</returns>
        public ISurvey GetSurvey()
        {
            return this.Survey;
        }

        /// <summary>
        /// Gets the questions.
        /// </summary>
        /// <returns>Array of IQuestions</returns>
        public List<IQuestion> GetQuestions()
        {
            var questions = new List<IQuestion>();

            foreach (Question q in Questions)
            {
                questions.Add(q);
            }

            questions.Sort(new Question.RelativeOrderComparer());

            return questions;
        }

        /// <summary>
        /// Renders the specified ph.
        /// </summary>
        /// <param name="placeHolder">The place holder.</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <param name="showRequiredNotation">if set to <c>true</c> [show required notation].</param>
        /// <param name="validationProvider">The validation provider.</param>
        /// <param name="localizer">The text for the default option (signifying no choice).</param>
        public virtual void Render(PlaceHolder placeHolder, bool readOnly, bool showRequiredNotation, ValidationProviderBase validationProvider, ILocalizer localizer)
        {
            RenderSection(this, placeHolder, readOnly, showRequiredNotation, validationProvider, localizer);
        }

        /// <summary>
        /// Renders the specified ph.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="ph">The placeholder container.</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <param name="showRequiredNotation">if set to <c>true</c> [show required notation].</param>
        /// <param name="validationProvider">The validation provider.</param>
        /// <param name="localizer">Localizes text.</param>
        public static void RenderSection(ISection section, PlaceHolder ph, bool readOnly, bool showRequiredNotation, ValidationProviderBase validationProvider, ILocalizer localizer)
        {
            var sectionDiv = new HtmlGenericControl("DIV");
            sectionDiv.Attributes["class"] = Engage.Survey.Util.Utility.CssClassSectionWrap + " section" + section.SectionId;
            ph.Controls.Add(sectionDiv);

            // row for the section text
            var title = new HtmlGenericControl("h3");
            title.Attributes["class"] = Engage.Survey.Util.Utility.CssClassSectionTitle;
            title.InnerHtml = HttpUtility.HtmlEncode(section.FormattedText).Replace("\n", "<br />");
            sectionDiv.Controls.Add(title);

            foreach (IQuestion question in section.GetQuestions())
            {
                // create the question wrap div.
                var questionWrapDiv = new HtmlGenericControl("DIV");
                questionWrapDiv.Attributes["class"] = "qw" + question.QuestionId;
                sectionDiv.Controls.Add(questionWrapDiv);

                // create the question span with label in it for question text.
                var questionSpan = new HtmlGenericControl("SPAN");
                questionSpan.Attributes["class"] = Engage.Survey.Util.Utility.CssClassQuestion;
                questionSpan.InnerHtml = question.FormattedText;
                questionWrapDiv.Controls.Add(questionSpan);

                // <span class="questin">Phone<span>*</span></span>
                // if the question is required, then add the optional * notation.
                if (question.IsRequired && showRequiredNotation)
                {
                    var requiredSpan = new HtmlGenericControl("SPAN");
                    requiredSpan.Attributes["class"] = Engage.Survey.Util.Utility.CssClassRequired;
                    requiredSpan.InnerText = localizer.Localize("RequiredNotation.Text");
                    questionSpan.Controls.Add(requiredSpan);
                }

                // Create a span to put answer(s) in.
                Control control = Engage.Survey.Util.Utility.CreateWebControl(question, readOnly, string.Empty, localizer);
                questionWrapDiv.Controls.Add(control);

                if (string.IsNullOrEmpty(control.ID) || validationProvider == null)
                {
                    continue;
                }

                // TODO: Localize error messages
                if (question.IsRequired && question.ControlType != ControlType.Checkbox)
                {
                    validationProvider.RegisterValidator(ph.Page.ClientScript, ValidationType.RequiredField, "error-message", questionWrapDiv, control.ID, string.Format(localizer.Localize("RequiredError.Format"), question.UnformattedText), 1, 0);
                }

                switch (question.ControlType)
                {
                    case ControlType.LargeTextInputField:
                    case ControlType.SmallTextInputField:
                        validationProvider.RegisterValidator(ph.Page.ClientScript, ValidationType.LimitedLengthField, "error-message", questionWrapDiv, control.ID, string.Format(localizer.Localize("TextLengthExceeded.Format"), question.UnformattedText), 1, 4000);
                        break;
                    case ControlType.EmailInputField:
                        validationProvider.RegisterValidator(ph.Page.ClientScript, ValidationType.EmailField, "error-message", questionWrapDiv, control.ID, localizer.Localize("InvalidEmail.Text"), 1, 0);
                        break;
                    case ControlType.Checkbox:
                        validationProvider.RegisterValidator(ph.Page.ClientScript, ValidationType.LimitedSelection, "error-message", questionWrapDiv, control.ID, string.Empty, question.SelectionLimit, 0);
                        break;
                }
            }
        }

        /// <summary>
        /// Posts the save processing.
        /// </summary>
        /// <param name="control">The control.</param>
        public virtual void PostSaveProcessing(WebControl control)
        {
            // no behavior, subclasses must define, if required
        }

        /// <summary>
        /// Pres the save processing.
        /// </summary>
        /// <param name="control">The control.</param>
        public virtual void PreSaveProcessing(WebControl control)
        {
            // no behavior, subclasses must define, if required
        }

        #endregion
        
        /// <summary>
        /// RelativeOrderComparer class
        /// </summary>
        internal class RelativeOrderComparer : IComparer<ISection>
        {
            /// <summary>
            /// ASC or DESC
            /// </summary>
            private readonly bool descending = true;

            /// <summary>
            /// Initializes a new instance of the <see cref="RelativeOrderComparer"/> class.
            /// </summary>
            public RelativeOrderComparer()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RelativeOrderComparer"/> class.
            /// </summary>
            /// <param name="descending">if set to <c>true</c> [descending].</param>
            public RelativeOrderComparer(bool descending)
            {
                this.descending = descending;
            }

            #region IComparer Members
            
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value
            /// Condition
            /// Less than zero
            /// <paramref name="x"/> is less than <paramref name="y"/>.
            /// Zero
            /// <paramref name="x"/> equals <paramref name="y"/>.
            /// Greater than zero
            /// <paramref name="x"/> is greater than <paramref name="y"/>.
            /// </returns>
            public int Compare(ISection x, ISection y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                if (this.descending)
                {
                    return x.RelativeOrder.CompareTo(y.RelativeOrder);
                }

                return y.RelativeOrder.CompareTo(x.RelativeOrder);
            }

            #endregion
        }
    }
}