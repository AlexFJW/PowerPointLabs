﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using PowerPointLabs.Models;
using PowerPointLabs.Utils;
using PowerPointLabs.Views;
using Graphics = PowerPointLabs.Utils.Graphics;
using Shape = Microsoft.Office.Interop.PowerPoint.Shape;

namespace PowerPointLabs.AgendaLab2
{
    /// <summary>
    /// The sections should not change during generation / syncing.
    /// </summary>
    internal static partial class AgendaLabMain
    {
        private static LoadingDialog _loadDialog = new LoadingDialog();

        private const int SectionNameMaxLength = 180;


        #region Bullet Formats
        private struct BulletFormats
        {
            public readonly TextRange2 Visited;
            public readonly TextRange2 Highlighted;
            public readonly TextRange2 Unvisited;

            private BulletFormats(TextRange2 visited, TextRange2 highlighted, TextRange2 unvisited)
            {
                Visited = visited;
                Highlighted = highlighted;
                Unvisited = unvisited;
            }

            /// <summary>
            /// Assumes the number of paragraphs >= 3.
            /// The check should have been done before this function is called.
            /// </summary>
            public static BulletFormats ExtractFormats(Shape contentShape)
            {
                var paragraphs = contentShape.TextFrame2.TextRange.Paragraphs.Cast<TextRange2>().ToList();
                return new BulletFormats(paragraphs[0],
                                        paragraphs[1],
                                        paragraphs[2]);
            }
        }
        #endregion

        #region Beam Formats
        private struct BeamFormats
        {
            public readonly TextRange2 Highlighted;
            public readonly TextRange2 Regular;

            private BeamFormats(TextRange2 highlighted, TextRange2 regular)
            {
                Highlighted = highlighted;
                Regular = regular;
            }

            /// <summary>
            /// Assumes that a "Highlighted Format" textbox exists,
            /// and that there is at last one regular textbox.
            /// </summary>
            public static BeamFormats ExtractFormats(Shape beamShape)
            {
                var beamText = GetShapeWithPurpose(beamShape, ShapePurpose.BeamShapeHighlightedText);
                var regularText = GetShapeWithPurpose(beamShape, ShapePurpose.BeamShapeText);
                return new BeamFormats(beamText.TextFrame2.TextRange, regularText.TextFrame2.TextRange);
            }

            public static Shape GetShapeWithPurpose(Shape beamShape, ShapePurpose purpose)
            {
                return beamShape.GroupItems.Cast<Shape>().FirstOrDefault(AgendaShape.WithPurpose(purpose));
            }

            public static List<Shape> GetAllShapesWithPurpose(Shape beamShape, ShapePurpose purpose)
            {
                return beamShape.GroupItems.Cast<Shape>().Where(AgendaShape.WithPurpose(purpose)).ToList();
            }
        }
        #endregion

        #region API
        public static void GenerateAgenda(Type type)
        {
            bool dialogOpen = false;
            var currentWindow = Globals.ThisAddIn.Application.ActiveWindow;
            var oldViewType = currentWindow.ViewType;

            try
            {
                var slideTracker = new SlideSelectionTracker(SelectedSlides, CurrentSlide);

                if (AgendaPresent())
                {
                    var confirm = MessageBox.Show(TextCollection.AgendaLabAgendaExistError,
                                                  TextCollection.AgendaLabAgendaExistErrorCaption,
                                                  MessageBoxButtons.OKCancel);
                    if (confirm != DialogResult.OK) return;

                    RemoveAllAgendaItems(slideTracker);
                }

                if (!ValidSections()) return;

                slideTracker.DeleteAcknowledgementSlideAndTrack();

                dialogOpen = DisplayLoadingDialog(TextCollection.AgendaLabGeneratingDialogTitle,
                                                    TextCollection.AgendaLabGeneratingDialogContent);
                currentWindow.ViewType = PpViewType.ppViewNormal;

                switch (type)
                {
                    case Type.Beam:
                        CreateBeamAgenda(slideTracker);
                        break;
                    case Type.Bullet:
                        CreateBulletAgenda(slideTracker);
                        break;
                    case Type.Visual:
                        CreateVisualAgenda(slideTracker);
                        break;
                }

                PowerPointPresentation.Current.AddAckSlide();
                SelectOriginalSlide(slideTracker.UserCurrentSlide, PowerPointPresentation.Current.FirstSlide);
            }
            finally
            {
                if (dialogOpen)
                {
                    DisposeLoadingDialog();
                }
                currentWindow.ViewType = oldViewType;
            }
        }

        public static void RemoveAgenda()
        {
            var currentWindow = Globals.ThisAddIn.Application.ActiveWindow;
            var oldViewType = currentWindow.ViewType;

            try
            {
                var slideTracker = new SlideSelectionTracker(SelectedSlides, CurrentSlide);

                if (!AgendaPresent())
                {
                    MessageBox.Show(TextCollection.AgendaLabNoAgendaError);
                    return;
                }
                currentWindow.ViewType = PpViewType.ppViewNormal;

                RemoveAllAgendaItems(slideTracker);

                SelectOriginalSlide(slideTracker.UserCurrentSlide, PowerPointPresentation.Current.FirstSlide);
            }
            finally
            {
                currentWindow.ViewType = oldViewType;
            }
        }

        public static void SynchroniseAgenda()
        {
            bool dialogOpen = false;
            var currentWindow = Globals.ThisAddIn.Application.ActiveWindow;
            var oldViewType = currentWindow.ViewType;

            try
            {
                var slideTracker = new SlideSelectionTracker(SelectedSlides, CurrentSlide);
                var refSlide = FindReferenceSlide();
                var type = GetReferenceSlideType();

                if (!ValidAgenda(refSlide, type)) return;
                if (!ValidSections()) return;

                slideTracker.DeleteAcknowledgementSlideAndTrack();
                dialogOpen = DisplayLoadingDialog(TextCollection.AgendaLabSynchronizingDialogTitle,
                                                    TextCollection.AgendaLabSynchronizingDialogContent);
                currentWindow.ViewType = PpViewType.ppViewNormal;

                BringToFront(refSlide);

                switch (type)
                {
                    case Type.Beam:
                        SyncBeamAgenda(slideTracker, refSlide);
                        break;
                    case Type.Bullet:
                        SyncBulletAgenda(slideTracker, refSlide);
                        break;
                    case Type.Visual:
                        SyncVisualAgenda(slideTracker, refSlide);
                        break;
                }

                PowerPointPresentation.Current.AddAckSlide();
                SelectOriginalSlide(slideTracker.UserCurrentSlide, PowerPointPresentation.Current.FirstSlide);
            }
            finally
            {
                if (dialogOpen)
                {
                    DisposeLoadingDialog();
                }
                currentWindow.ViewType = oldViewType;
            }
        }

        #endregion


        #region Actions - Creation

        /// <summary>
        /// Assumption: no reference slide exists
        /// </summary>
        private static void CreateBulletAgenda(SlideSelectionTracker slideTracker)
        {
            var refSlide = CreateBulletReferenceSlide();

            // here we invoke sync logic, since it's the same behavior as sync
            SyncBulletAgendaSlides(slideTracker, refSlide);
        }


        /// <summary>
        /// Assumption: no reference slide exists
        /// </summary>
        private static void CreateVisualAgenda(SlideSelectionTracker slideTracker)
        {
            var refSlide = CreateVisualReferenceSlide();

            // here we invoke sync logic, since it's the same behavior as sync
            SyncVisualAgendaSlides(slideTracker, refSlide);
        }


        /// <summary>
        /// Assumption: no reference slide exists
        /// </summary>
        private static void CreateBeamAgenda(SlideSelectionTracker slideTracker)
        {
            var refSlide = CreateBeamReferenceSlide();

            // here we invoke sync logic, since it's the same behavior as sync
            var targetSlides = slideTracker.SelectedSlides;
            if (targetSlides.Count == 0)
            {
                targetSlides = AllSlidesAfterFirstSection();
            }

            SyncBeamOnSlides(targetSlides, refSlide);
        }

        #endregion


        #region Reference Slide Creation - Beam

        private static PowerPointSlide CreateBeamReferenceSlide()
        {
            var refSlide = PowerPointSlide.FromSlideFactory(PowerPointPresentation.Current
                                                            .Presentation
                                                            .Slides
                                                            .Add(1, PpSlideLayout.ppLayoutBlank));

            CreateBeamAgendaShapes(refSlide);

            AgendaSlide.SetAsReferenceSlideName(refSlide, Type.Beam);
            refSlide.AddTemplateSlideMarker();
            refSlide.Hidden = true;

            return refSlide;
        }

        private static void CreateBeamAgendaShapes(PowerPointSlide refSlide, Direction beamDirection = Direction.Top)
        {
            var sections = GetAllButFirstSection();

            var background = PrepareBeamAgendaBackground(refSlide);
            var textBoxes = CreateBeamAgendaTextBoxes(refSlide, sections);
            SetupBeamTextBoxPositions(textBoxes, background);

            var highlightedTextBox = CreateHighlightedTextBox(0, 1f, refSlide);

            var beamShapeItems = new List<Shape>();
            beamShapeItems.Add(background);
            beamShapeItems.Add(highlightedTextBox);
            beamShapeItems.AddRange(textBoxes);

            var group = refSlide.GroupShapes(beamShapeItems);
            AgendaShape.SetShapeName(group, ShapePurpose.BeamShapeMainGroup, AgendaSection.None);
        }

        private static List<Shape> CreateBeamAgendaTextBoxes(PowerPointSlide refSlide, List<AgendaSection> sections)
        {
            return sections.Select(section => PrepareBeamAgendaBeamItem(refSlide, section)).ToList();
        }

        private static void SetupBeamTextBoxPositions(List<Shape> textBoxes, Shape background = null)
        {
            var slideWidth = PowerPointPresentation.Current.SlideWidth;
            var slideHeight = PowerPointPresentation.Current.SlideHeight;
            float itemWidth = textBoxes.Select(textBox => textBox.Width).Max();
            float itemHeight = textBoxes.Select(textBox => textBox.Height).Max();

            var spacing = Math.Max(itemWidth, slideWidth/textBoxes.Count);
            int columnCount = (int) (slideWidth/spacing + 0.01f); // +0.01f to cater to rounding errors.
            int rowCount = Common.CeilingDivide(textBoxes.Count, columnCount);

            var left = 0f;
            var top = 0f;

            for (int i = 0; i < textBoxes.Count; ++i)
            {
                int x = i%columnCount;
                int y = i/columnCount;

                var textBox = textBoxes[i];
                textBox.Left = left + x*spacing + (spacing - textBox.Width)/2;
                textBox.Top = top + y*itemHeight;
            }

            if (background != null)
            {
                background.Left = 0f;
                background.Top = 0f;
                background.Height = rowCount*itemHeight;
                background.Width = slideWidth;
            }
        }

        private static Shape CreateHighlightedTextBox(float left, float top, PowerPointSlide slide)
        {
            var textBox = slide.Shapes.AddTextbox(MsoTextOrientation.msoTextOrientationHorizontal,
                                                  left, top, 0, 0);

            AgendaShape.SetShapeName(textBox, ShapePurpose.BeamShapeHighlightedText, AgendaSection.None);
            textBox.TextFrame.AutoSize = PpAutoSize.ppAutoSizeShapeToFitText;
            textBox.TextFrame.WordWrap = MsoTriState.msoFalse;
            textBox.TextFrame.TextRange.Text = TextCollection.AgendaLabBeamHighlightedText;
            textBox.TextFrame.TextRange.Font.Color.RGB = Graphics.ConvertColorToRgb(Color.Yellow);

            return textBox;
        }

        private static Shape PrepareBeamAgendaBackground(PowerPointSlide slide)
        {
            var background = slide.Shapes.AddShape(MsoAutoShapeType.msoShapeRectangle, 0, 0, 0, 0);

            AgendaShape.SetShapeName(background, ShapePurpose.BeamShapeBackground, AgendaSection.None);
            background.Line.Visible = MsoTriState.msoFalse;
            background.Fill.ForeColor.RGB = Graphics.ConvertColorToRgb(Color.Black);
            background.Width = PowerPointPresentation.Current.SlideWidth;

            return background;
        }

        private static Shape PrepareBeamAgendaBeamItem(PowerPointSlide slide, AgendaSection section)
        {
            var textBox = slide.Shapes.AddTextbox(MsoTextOrientation.msoTextOrientationHorizontal, 0, 0, 0, 0);

            AgendaShape.SetShapeName(textBox, ShapePurpose.BeamShapeText, section);
            textBox.TextFrame.AutoSize = PpAutoSize.ppAutoSizeShapeToFitText;
            textBox.TextFrame.WordWrap = MsoTriState.msoFalse;
            textBox.TextFrame.TextRange.Text = section.Name;
            textBox.TextFrame.TextRange.Font.Color.RGB = Graphics.ConvertColorToRgb(Color.White);

            return textBox;
        }

        #endregion


        #region Reference Slide Creation - Bullet

        private static PowerPointSlide CreateBulletReferenceSlide()
        {
            var refSlide = PowerPointSlide.FromSlideFactory(PowerPointPresentation.Current
                                                            .Presentation
                                                            .Slides
                                                            .Add(1, PpSlideLayout.ppLayoutText));

            var titleShape = refSlide.Shapes.Placeholders[1];
            var contentShape = refSlide.Shapes.Placeholders[2];
            AgendaShape.SetShapeName(titleShape, ShapePurpose.TitleShape, AgendaSection.None);
            AgendaShape.SetShapeName(contentShape, ShapePurpose.ContentShape, AgendaSection.None);

            Graphics.SetText(titleShape, TextCollection.AgendaLabTitleContent);
            Graphics.SetText(contentShape, TextCollection.AgendaLabBulletVisitedContent,
                                            TextCollection.AgendaLabBulletHighlightedContent,
                                            TextCollection.AgendaLabBulletUnvisitedContent);

            var paragraphs = Graphics.GetParagraphs(contentShape);
            paragraphs[0].Font.Fill.ForeColor.RGB = Graphics.ConvertColorToRgb(Color.Gray);
            paragraphs[1].Font.Fill.ForeColor.RGB = Graphics.ConvertColorToRgb(Color.Red);
            paragraphs[2].Font.Fill.ForeColor.RGB = Graphics.ConvertColorToRgb(Color.Black);

            AgendaSlide.SetAsReferenceSlideName(refSlide, Type.Bullet);
            refSlide.AddTemplateSlideMarker();
            refSlide.Hidden = true;

            return refSlide;
        }

        #endregion


        #region Reference Slide Creation - Visual

        private static PowerPointSlide CreateVisualReferenceSlide()
        {
            var refSlide = PowerPointSlide.FromSlideFactory(PowerPointPresentation.Current
                                                            .Presentation
                                                            .Slides
                                                            .Add(1, PpSlideLayout.ppLayoutTitleOnly));

            var titleBar = refSlide.Shapes.Placeholders[1];
            AgendaShape.SetShapeName(titleBar, ShapePurpose.TitleShape, AgendaSection.None);
            Graphics.SetText(titleBar, TextCollection.AgendaLabTitleContent);

            InsertVisualAgendaSectionImages(refSlide);

            AgendaSlide.SetAsReferenceSlideName(refSlide, Type.Visual);
            refSlide.AddTemplateSlideMarker();
            refSlide.Hidden = true;

            return refSlide;
        }


        /// <summary>
        /// Inserts the section images into the reference slide in a nice square pattern and names them appropriately.
        /// </summary>
        private static void InsertVisualAgendaSectionImages(PowerPointSlide refSlide)
        {
            var sectionImages = CreateSectionImagesForAllSections(refSlide);
            ArrangeInGrid(sectionImages);
        }

        private static void ArrangeInGrid(List<Shape> sectionImages)
        {
            float slideWidth = PowerPointPresentation.Current.SlideWidth;
            float slideHeight = PowerPointPresentation.Current.SlideHeight;
            float aspectRatio = slideWidth/slideHeight;

            // These numbers can be tweaked.
            float panelFillRatio = 0.9f;
            float canvasTop = slideHeight*0.25f;
            float canvasBottom = slideHeight*0.85f;

            float canvasHeight = canvasBottom - canvasTop;
            float canvasWidth = aspectRatio*canvasHeight;
            float canvasLeft = (slideWidth - canvasWidth)/2;

            int columnCount = (int) Math.Ceiling(Math.Sqrt(sectionImages.Count));
            int rowCount = Common.CeilingDivide(sectionImages.Count, columnCount);
            float panelWidth = canvasWidth/columnCount;
            float panelHeight = panelWidth/aspectRatio;

            float pictureWidth = panelFillRatio*panelWidth;
            float pictureHeight = panelFillRatio*panelHeight;
            float pictureXOffset = canvasLeft + (panelWidth - pictureWidth)/2;
            float pictureYOffset = canvasTop + (canvasHeight - rowCount*panelHeight)/2 + (panelHeight - pictureHeight)/2;

            for (int i = 0; i < sectionImages.Count; ++i)
            {
                var sectionImage = sectionImages[i];
                int xPosition = i%columnCount;
                int yPosition = i/columnCount;

                sectionImage.Left = pictureXOffset + xPosition*panelWidth;
                sectionImage.Top = pictureYOffset + yPosition*panelHeight;
                sectionImage.Width = pictureWidth;
                sectionImage.Height = pictureHeight;
            }
        }

        private static List<Shape> CreateSectionImagesForAllSections(PowerPointSlide refSlide)
        {
            var sections = GetAllButFirstSection();
            var sectionImages = new List<Shape>();
            foreach (var section in sections)
            {
                var sectionImage = CreateSectionImage(refSlide, section);
                sectionImages.Add(sectionImage);
            }
            return sectionImages;
        }
        

        private static Shape CreateSectionImage(PowerPointSlide refSlide, AgendaSection section)
        {
            var sectionFirstSlide = FindSectionFirstNonAgendaSlide(section.Index);
            var shape = refSlide.InsertEntrySnapshotOfSlide(sectionFirstSlide);
            AgendaShape.SetShapeName(shape, ShapePurpose.VisualAgendaImage, section);
            return shape;
        }


        private static void UpdateSectionImage(PowerPointSlide refSlide, AgendaSection section, Shape previousImageShape)
        {
            var snapshotShape = CreateSectionImage(refSlide, section);
            Graphics.SyncShape(previousImageShape, snapshotShape, pickupShapeFormat: false, pickupTextContent: false, pickupTextFormat: false);
            previousImageShape.Delete();
        }
        
        #endregion


        #region Actions - Removal

        private static void RemoveAllAgendaItems(SlideSelectionTracker slideTracker = null)
        {
            if (slideTracker == null) slideTracker = SlideSelectionTracker.CreateInactiveTracker();

            PowerPointPresentation.Current.Slides.Where(AgendaSlide.IsAnyAgendaSlide)
                                                 .ToList()
                                                 .ForEach(slideTracker.DeleteSlideAndTrack);

            PowerPointPresentation.Current.Slides.ToList()
                                                 .ForEach(RemoveBeamAgendaFromSlide);
        }

        private static void RemoveBeamAgendaFromSlide(PowerPointSlide slide)
        {
            slide.Shapes.Cast<Shape>()
                        .Where(AgendaShape.WithPurpose(ShapePurpose.BeamShapeMainGroup))
                        .ToList()
                        .ForEach(shape => shape.Delete());
        }

        #endregion


        #region Actions - Synchronise - General

        private static void BringToFront(PowerPointSlide slide)
        {
            slide.MoveTo(1);
        }

        /// <summary>
        /// Scrambles the slide section names to avoid duplicate names later on, which can crash powerpoint.
        /// Use this just before reassigning the slide section names! Don't keep the slide names this way!
        /// </summary>
        private static void ScrambleSlideSectionNames()
        {
            var slides = PowerPointPresentation.Current.Slides;
            slides.Where(slide => AgendaSlide.IsAnyAgendaSlide(slide) && AgendaSlide.IsNotReferenceslide(slide))
                    .ToList()
                    .ForEach(AgendaSlide.AssignUniqueSectionName);
        }
        #endregion

        #region Actions - Synchronise - Bullet

        /// <summary>
        /// Called from the Synchronise action only.
        /// </summary>
        private static void SyncBulletAgenda(SlideSelectionTracker slideTracker, PowerPointSlide refSlide)
        {
            AdjustBulletReferenceSlideContent(refSlide);
            SyncBulletAgendaSlides(slideTracker, refSlide);
        }

        /// <summary>
        /// Called from both generate and synchronise actions.
        /// </summary>
        private static void SyncBulletAgendaSlides(SlideSelectionTracker slideTracker, PowerPointSlide refSlide)
        {
            var sections = Sections;

            ScrambleSlideSectionNames();
            foreach (var currentSection in sections)
            {
                var template = new BulletAgendaTemplate();
                ConfigureTemplate(currentSection, template);

                var templateTable = RebuildSectionUsingTemplate(slideTracker, currentSection, template);
                SynchroniseAllSlides(template, templateTable, refSlide, sections, currentSection);
            }
        }

        private static void AdjustBulletReferenceSlideContent(PowerPointSlide refSlide)
        {
            int numberOfSections = NumberOfSections;

            // post process bullet points
            var contentHolder = refSlide.GetShape(AgendaShape.WithPurpose(ShapePurpose.ContentShape));
            var textRange = contentHolder.TextFrame2.TextRange;

            while (textRange.Paragraphs.Count < numberOfSections)
            {
                textRange.InsertAfter("\r ");
            }

            while (textRange.Paragraphs.Count > 3 && textRange.Paragraphs.Count > numberOfSections)
            {
                textRange.Paragraphs[textRange.Paragraphs.Count].Delete();
            }

            for (var i = 4; i <= textRange.Paragraphs.Count; i++)
            {
                textRange.Paragraphs[i].ParagraphFormat.Bullet.Type = MsoBulletType.msoBulletNone;
            }
        }

        #endregion

        #region Actions - Synchronise - Visual

        /// <summary>
        /// Called from the Synchronise action only.
        /// </summary>
        private static void SyncVisualAgenda(SlideSelectionTracker slideTracker, PowerPointSlide refSlide)
        {
            RegenerateReferenceSlideImages(refSlide);
            SyncVisualAgendaSlides(slideTracker, refSlide);
        }

        /// <summary>
        /// Called from both generate and synchronise actions.
        /// </summary>
        private static void SyncVisualAgendaSlides(SlideSelectionTracker slideTracker, PowerPointSlide refSlide)
        {
            var sections = Sections;

            DeleteAllZoomSlides(slideTracker);
            ScrambleSlideSectionNames();
            foreach (var currentSection in sections)
            {
                var template = new VisualAgendaTemplate();
                ConfigureTemplate(currentSection, template);

                var templateTable = RebuildSectionUsingTemplate(slideTracker, currentSection, template);
                SynchroniseAllSlides(template, templateTable, refSlide, sections, currentSection);
            }
        }

        private static void RegenerateReferenceSlideImages(PowerPointSlide refSlide)
        {
            List<Shape> markedForDeletion;
            var shapeAssignment = GetImageShapeAssignment(refSlide, out markedForDeletion);

            var sections = GetAllButFirstSection();
            var assignedOldIndexes = new HashSet<int>();
            var unassignedNewSections = new List<AgendaSection>();


            float existingImageWidth = -1;
            float existingImageHeight = -1;

            foreach (var section in sections)
            {
                int oldIndex = IdentifyOldSectionIndex(section);
                if (oldIndex == -1 || assignedOldIndexes.Contains(oldIndex))
                {
                    unassignedNewSections.Add(section);
                    continue;
                }
                Shape imageShape;
                bool canFindShape = shapeAssignment.TryGetValue(oldIndex, out imageShape);
                if (!canFindShape)
                {
                    unassignedNewSections.Add(section);
                    continue;
                }

                existingImageWidth = imageShape.Width;
                existingImageHeight = imageShape.Height;

                UpdateSectionImage(refSlide, section, imageShape);
                assignedOldIndexes.Add(oldIndex);
                
            }

            markedForDeletion.AddRange(from entry in shapeAssignment where !assignedOldIndexes.Contains(entry.Key) select entry.Value);

            var newSectionImages = unassignedNewSections.Select(section => CreateSectionImage(refSlide, section))
                                                        .ToList();
            PositionNewImageShapes(newSectionImages, existingImageWidth, existingImageHeight);

            markedForDeletion.ForEach(shape => shape.Delete());
        }

        private static Dictionary<int, Shape> GetImageShapeAssignment(PowerPointSlide inSlide, out List<Shape> unassignedShapes)
        {
            var shapes = inSlide.Shapes.Cast<Shape>();

            unassignedShapes = new List<Shape>();
            var shapeAssignment = new Dictionary<int, Shape>();

            foreach (var shape in shapes)
            {
                var agendaShape = AgendaShape.Decode(shape);
                if (agendaShape == null || agendaShape.ShapePurpose != ShapePurpose.VisualAgendaImage) continue;

                int index = agendaShape.Section.Index;
                if (shapeAssignment.ContainsKey(index))
                {
                    unassignedShapes.Add(shape);
                }
                else
                {
                    shapeAssignment.Add(index, shape);
                }
            }

            return shapeAssignment;
        }

        /// <summary>
        /// Places the newly generated image shapes in some alignment that makes them easy to drag around.
        /// Resizes image shapes to match the sizes of the existing image shapes.
        /// If existingImageWidth <= 0 or existingImageHeight <= 0, it means there are no already existing image shapes.
        /// </summary> 
        private static void PositionNewImageShapes(List<Shape> shapes, float existingImageWidth, float existingImageHeight)
        {
            if (shapes.Count == 0) return;

            ArrangeInGrid(shapes);
            if (existingImageWidth <= 0 || existingImageHeight <= 0) return;

            foreach (var shape in shapes)
            {
                shape.Width = existingImageWidth;
                shape.Height = existingImageHeight;
            }
        }

        /// <summary>
        /// Identifies the previous section index of a section by looking at the generated agenda slides in the section.
        /// The identified section is the section index of the first generated agenda slide found.
        /// Returns -1 when old section index is not found.
        /// </summary>
        private static int IdentifyOldSectionIndex(AgendaSection section)
        {
            var sectionSlides = GetSectionSlides(section);
            foreach (var slide in sectionSlides)
            {
                var agendaSlide = AgendaSlide.Decode(slide);
                if (agendaSlide != null)
                {
                    return agendaSlide.Section.Index;
                }
            }
            return -1;
        }

        private static void DeleteAllZoomSlides(SlideSelectionTracker slideTracker)
        {
            PowerPointPresentation.Current.Slides
                                        .Where(AgendaSlide.MeetsConditions(slide => slide.SlidePurpose == SlidePurpose.ZoomIn ||
                                                                                    slide.SlidePurpose == SlidePurpose.ZoomOut ||
                                                                                    slide.SlidePurpose == SlidePurpose.FinalZoomOut))
                                        .ToList()
                                        .ForEach(slideTracker.DeleteSlideAndTrack);
        }

        #endregion

        #region Actions - Synchronise - Beam

        private static void SyncBeamAgenda(SlideSelectionTracker slideTracker, PowerPointSlide refSlide)
        {
            UpdateBeamReferenceSlide(refSlide);

            SyncBeamOnSlides(slideTracker.SelectedSlides, refSlide);
        }

        private static void UpdateBeamReferenceSlide(PowerPointSlide refSlide)
        {
            var beamShape = FindBeamShape(refSlide);
            var currentSections = ExtractAgendaSectionsFromBeam(beamShape);
            var newSections = GetAllButFirstSection();

            if (SectionsMatch(currentSections, newSections)) return;

            var beamFormats = BeamFormats.ExtractFormats(beamShape);
            var oldTextBoxes = BeamFormats.GetAllShapesWithPurpose(beamShape, ShapePurpose.BeamShapeText);
            var background = BeamFormats.GetShapeWithPurpose(beamShape, ShapePurpose.BeamShapeBackground);

            var newTextBoxes = CreateBeamAgendaTextBoxes(refSlide, newSections);
            SetupBeamTextBoxPositions(newTextBoxes, background);

            for (int i = 0; i < newTextBoxes.Count; ++i)
            {
                var referenceTextFormat = beamFormats.Regular;
                if (i < oldTextBoxes.Count) referenceTextFormat = oldTextBoxes[i].TextFrame2.TextRange;

                Graphics.SyncTextRange(referenceTextFormat, newTextBoxes[i].TextFrame2.TextRange, pickupTextContent: false);
            }

            oldTextBoxes.ForEach(shape => shape.Delete());

            var beamShapeShapes = beamShape.Ungroup().Cast<Shape>().ToList();
            beamShapeShapes.AddRange(newTextBoxes);
            beamShape = refSlide.GroupShapes(beamShapeShapes);
            AgendaShape.SetShapeName(beamShape, ShapePurpose.BeamShapeMainGroup, AgendaSection.None);
        }

        private static bool SectionsMatch(List<AgendaSection> currentSections, List<AgendaSection> newSections)
        {
            if (currentSections == null) return false;
            if (currentSections.Count != newSections.Count) return false;
            for (int i = 0; i < currentSections.Count; ++i)
            {
                var currentSection = currentSections[i];
                var newSection = newSections[i];
                if (currentSection.Index != newSection.Index || currentSection.Name != newSection.Name) return false;
            }
            return true;
        }

        /// <summary>
        /// Extracts a list of the current sections from textboxes of the beamshape.
        /// If the textboxes are not consistent (e.g. repeated or missing section), returns null instead.
        /// </summary>
        private static List<AgendaSection> ExtractAgendaSectionsFromBeam(Shape beamShape)
        {
            var agendaSections = beamShape.GroupItems.Cast<Shape>()
                                                    .Where(AgendaShape.WithPurpose(ShapePurpose.BeamShapeText))
                                                    .Select(shape => AgendaShape.Decode(shape).Section)
                                                    .ToList();

            agendaSections.Sort((s1, s2) => s1.Index - s2.Index);

            for (int i = 0; i < agendaSections.Count; ++i)
            {
                if (agendaSections[i].Index != i + 2)
                {
                    return null;
                }
            }
            return agendaSections;
        }


        private static void SyncBeamOnSlides(List<PowerPointSlide> targetSlides, PowerPointSlide refSlide)
        {
            var syncSlides = new List<PowerPointSlide>();

            // Generate beam agenda for all target slides that do not currently have the beam agenda.
            if (targetSlides != null)
            {
                var selectedSlidesWithoutBeam = targetSlides.Where(slide => !HasBeamShape(slide));
                syncSlides.AddRange(selectedSlidesWithoutBeam);
            }

            // Synchronise agenda for all slides in the presentation that have the beam agenda.
            var refBeamShape = FindBeamShape(refSlide);
            var allSlidesWithBeam = PowerPointPresentation.Current.Slides
                                                                  .Where(slide => AgendaSlide.IsNotReferenceslide(slide) &&
                                                                                  FindBeamShape(slide) != null);
            syncSlides.AddRange(allSlidesWithBeam);

            foreach (var slide in syncSlides)
            {
                UpdateBeamOnSlide(slide, refBeamShape);
            }
        }

        private static void UpdateBeamOnSlide(PowerPointSlide slide, Shape refBeamShape)
        {
            RemoveBeamAgendaFromSlide(slide);
            refBeamShape.Copy();
            var beamShape = slide.Shapes.Paste();
            var section = GetSlideSection(slide);

            beamShape.GroupItems.Cast<Shape>()
                                .Where(AgendaShape.WithPurpose(ShapePurpose.BeamShapeHighlightedText))
                                .ToList()
                                .ForEach(shape => shape.Delete());

            if (section.Index == 1) return;

            var beamFormats = BeamFormats.ExtractFormats(refBeamShape);
            var currentSectionTextBox = beamShape.GroupItems
                                                .Cast<Shape>()
                                                .Where(AgendaShape.MeetsConditions(shape => shape.ShapePurpose == ShapePurpose.BeamShapeText &&
                                                                                            shape.Section.Index == section.Index))
                                                .FirstOrDefault();
            var currentSectionText = currentSectionTextBox.TextFrame2.TextRange;

            Graphics.SyncTextRange(beamFormats.Highlighted, currentSectionText, pickupTextContent: false);
        }
        #endregion


        #region Actions - General

        private static void SelectOriginalSlide(PowerPointSlide originalSlide, PowerPointSlide fallbackToSlide)
        {
            if (originalSlide != null)
            {
                originalSlide.GetNativeSlide().Select();
                return;
            }
            if (fallbackToSlide != null)
            {
                fallbackToSlide.GetNativeSlide().Select();
            }
        }

        private static bool DisplayLoadingDialog(string title, string content)
        {
            _loadDialog = new LoadingDialog(title, content);
            _loadDialog.Show();
            _loadDialog.Refresh();
            return true;
        }

        private static void DisposeLoadingDialog()
        {
            _loadDialog.Dispose();
        }

        #endregion


        #region Conditions on current state

        private static bool ValidSections()
        {
            var sections = PowerPointPresentation.Current.Sections;

            if (sections.Count == 0)
            {
                MessageBox.Show(TextCollection.AgendaLabNoSectionError);
                return false;
            }

            if (sections.Count == 1)
            {
                MessageBox.Show(TextCollection.AgendaLabSingleSectionError);
                return false;
            }

            if (HasEmptySection())
            {
                MessageBox.Show(TextCollection.AgendaLabEmptySectionError);
                return false;
            }

            if (HasTooLongSectionName())
            {
                MessageBox.Show(TextCollection.AgendaLabSectionNameTooLongError);
                return false;
            }

            return true;
        }

        private static bool HasTooLongSectionName()
        {
            var sections = Sections;
            return sections.Any(section => section.Name.Length > SectionNameMaxLength);
        }

        /// <summary>
        /// Checks whether there is a section with no slides.
        /// Agenda slides are not counted.
        /// </summary>
        private static bool HasEmptySection()
        {
            var sections = Sections;
            foreach (var section in sections)
            {
                var sectionSlides = GetSectionSlides(section);
                if (sectionSlides.All(slide => AgendaSlide.IsAnyAgendaSlide(slide) || PowerPointAckSlide.IsAckSlide(slide)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool AgendaPresent()
        {
            return FindAllAgendaSlides().Count > 0 || FindSlidesWithBeam().Count > 0;
        }

        private static bool ValidAgenda(PowerPointSlide refSlide, Type type)
        {
            if (!AgendaPresent())
            {
                MessageBox.Show(TextCollection.AgendaLabNoAgendaError);
                return false;
            }

            if (refSlide == null)
            {
                MessageBox.Show(TextCollection.AgendaLabNoReferenceSlideError);
                return false;
            }

            if (InvalidReferenceSlide(type, refSlide))
            {
                MessageBox.Show(TextCollection.AgendaLabInvalidReferenceSlideError);
                return false;
            }

            return true;
        }

        private static bool InvalidReferenceSlide(Type type, PowerPointSlide refSlide)
        {
            switch (type)
            {
                case Type.Beam:
                    return InvalidBeamAgendaReferenceSlide(refSlide);
                case Type.Bullet:
                    return InvalidBulletAgendaReferenceSlide(refSlide);
                case Type.Visual:
                    return InvalidVisualAgendaReferenceSlide(refSlide);
            }
            return true;
        }

        private static bool InvalidBulletAgendaReferenceSlide(PowerPointSlide refSlide)
        {
            var contentHolder = refSlide.GetShape(AgendaShape.WithPurpose(ShapePurpose.ContentShape));
            return (contentHolder == null || contentHolder.TextFrame2.TextRange.Paragraphs.Count < 3);
        }

        private static bool InvalidBeamAgendaReferenceSlide(PowerPointSlide refSlide)
        {
            var beamShape = FindBeamShape(refSlide);
            if (beamShape == null) return true;
            try
            {
                if (BeamFormats.GetShapeWithPurpose(beamShape, ShapePurpose.BeamShapeHighlightedText) == null)
                    return true;
                if (BeamFormats.GetShapeWithPurpose(beamShape, ShapePurpose.BeamShapeText) == null)
                    return true;
            }
            catch (COMException e)
            {
                // beam shape is not a group
                return true;
            }
            return false;
        }

        private static bool InvalidVisualAgendaReferenceSlide(PowerPointSlide refSlide)
        {
            return false;
        }

        #endregion


        private static string CreateInDocHyperLink(PowerPointSlide slide)
        {
            throw new NotImplementedException();
        }




        # region Event Handlers
        public static void SlideShowBeginHandler()
        {
            /*var type = CurrentType;

            if (type != Type.Bullet) return;

            var slides = PowerPointPresentation.Current.Slides.Where(AgendaSlide.IsAnyAgendaSlide);

            foreach (var slide in slides)
            {
                var linkShapes = slide.GetShapesWithPrefix(PptLabsAgendaBulletLinkShape);
                var contentHolder = slide.GetShapeWithName(PptLabsAgendaContentShapeName)[0];
                var textRange = contentHolder.TextFrame2.TextRange;

                if (linkShapes.Count == 0) return;

                for (var i = 1; i <= textRange.Paragraphs.Count; i++)
                {
                    var shape = linkShapes[i - 1];
                    var curPara = textRange.Paragraphs[i];

                    shape.Left = curPara.BoundLeft;
                    shape.Top = curPara.BoundTop;
                    shape.Width = curPara.BoundWidth;
                    shape.Height = curPara.BoundHeight;

                    shape.Visible = MsoTriState.msoTrue;
                }
            }*/
        }

        public static void SlideShowEndHandler()
        {
            /*var type = CurrentType;

            if (type != Type.Bullet) return;

            var slides = PowerPointPresentation.Current.Slides.Where(AgendaSlide.IsAnyAgendaSlide);

            foreach (var slide in slides)
            {
                var linkShapes = slide.GetShapesWithPrefix(PptLabsAgendaBulletLinkShape);

                foreach (var linkShape in linkShapes)
                {
                    linkShape.Visible = MsoTriState.msoFalse;
                }
            }*/
        }
        # endregion
    }

}
