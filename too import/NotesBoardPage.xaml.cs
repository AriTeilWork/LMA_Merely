using Microsoft.Maui.Controls;
using SQLite;
using System.Collections.ObjectModel;
using System.Linq;

namespace MauiNotes
{
    public partial class NotesBoardPage : ContentPage
    {
        SQLiteAsyncConnection? _db;
        ObservableCollection<NoteModel> Notes = new();
        View? _draggedItem;
        NoteModel? _draggedModel;

        public NotesBoardPage()
        {
            InitializeComponent();
            InitDb();
        }

        async void InitDb()
        {
            string path = System.IO.Path.Combine(FileSystem.AppDataDirectory, "notes.db3");
            _db = new SQLiteAsyncConnection(path);
            await _db.CreateTableAsync<NoteModel>();

            // If empty, add sample notes
            var all = await _db.Table<NoteModel>().ToListAsync();
            if (!all.Any())
            {
                var sample = new[]
                {
                    new NoteModel{ Title="Заметка 1", Text="Текст 1", State="todo"},
                    new NoteModel{ Title="Заметка 2", Text="Текст 2", State="doing"},
                    new NoteModel{ Title="Заметка 3", Text="Текст 3", State="done"},
                };
                foreach (var s in sample) await _db.InsertAsync(s);
                all = await _db.Table<NoteModel>().ToListAsync();
            }

            foreach (var n in all)
            {
                Notes.Add(n);
                AddNoteToColumn(n);
            }
        }

        void AddNoteToColumn(NoteModel note)
        {
            var frame = CreateNoteFrame(note);
            var col = note.State switch
            {
                "todo" => TodoColumn,
                "doing" => DoingColumn,
                _ => DoneColumn
            };
            col.Children.Add(frame);
        }

        Frame CreateNoteFrame(NoteModel note)
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.LightYellow,
                BorderColor = Colors.LightGoldenrodYellow,
                CornerRadius = 12,
                Padding = 12,
                Margin = 6,
                WidthRequest = 150,
                HeightRequest = 150
            };

            var layout = new VerticalStackLayout();
            layout.Children.Add(new Label { Text = note.Title, FontAttributes = FontAttributes.Bold, FontSize = 16 });
            layout.Children.Add(new Label { Text = note.Text, FontSize = 13 });

            var del = new Button { Text = "Удалить", FontSize = 12 };
            del.Clicked += async (s, e) =>
            {
                if (_db != null)
                {
                    await _db.DeleteAsync(note);
                }
                // remove from collection and UI
                var existing = Notes.FirstOrDefault(n => n.Id == note.Id);
                if (existing != null)
                {
                    Notes.Remove(existing);
                }
                RemoveNoteFromUI(note.Id);
            };

            layout.Children.Add(del);
            frame.Content = layout;

            // Drag
            var drag = new DragGestureRecognizer();
            drag.DragStarting += (s, e) =>
            {
                _draggedItem = frame;
                _draggedModel = note;
                _ = frame.ScaleTo(1.05, 120);
            };
            frame.GestureRecognizers.Add(drag);

            // Do not attach Drop to frame - columns handle drops

            return frame;
        }

        // New handler for column drops
        async void OnColumnDrop(object sender, DropEventArgs e)
        {
            if (_draggedItem == null || _draggedModel == null) return;

            var targetColumn = sender as VerticalStackLayout;
            if (targetColumn == null) return;

            // Remove from old parent
            var oldParent = _draggedItem.Parent as VerticalStackLayout;
            if (oldParent != null)
            {
                oldParent.Children.Remove(_draggedItem);
            }

            // Add to new parent
            targetColumn.Children.Add(_draggedItem);

            // Update model state and DB
            string newState = targetColumn == TodoColumn ? "todo" : targetColumn == DoingColumn ? "doing" : "done";
            var model = Notes.FirstOrDefault(x => x.Id == _draggedModel.Id);
            if (model != null)
            {
                model.State = newState;
                if (_db != null)
                {
                    await _db.UpdateAsync(model);
                }
            }

            // reset drag vars
            _ = _draggedItem.ScaleTo(1.0, 120);
            _draggedItem = null;
            _draggedModel = null;
        }

        void RemoveNoteFromUI(int id)
        {
            foreach (var col in new[] { TodoColumn, DoingColumn, DoneColumn })
            {
                var match = col.Children.FirstOrDefault(x => x is Frame f && ((VerticalStackLayout)f.Content).Children.OfType<Label>().FirstOrDefault()?.Text == Notes.FirstOrDefault(n => n.Id == id)?.Title);
                if (match != null)
                {
                    col.Children.Remove(match);
                    break;
                }
            }
        }
    }
}

namespace MauiNotes
{
    public class NoteModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty; // todo / doing / done
    }
}
