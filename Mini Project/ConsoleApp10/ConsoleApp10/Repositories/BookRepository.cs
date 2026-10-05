using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Repositories;

public class BookRepository
{
    private readonly List<Book> _books = new();

    public void Add(Book book)
    {
        _books.Add(book);
    }

    public List<Book> GetAll()
    {
        return _books;
    }

    public Book? GetById(int id)
    {
        return _books.FirstOrDefault(book => book.Id == id);
    }

    public Book? GetByTitle(string title)
    {
        return _books.FirstOrDefault(book =>
            book.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }

    public void Update(Book book)
    {
        var existingBook = GetById(book.Id);

        if (existingBook == null)
        {
            return;
        }

        existingBook.Title = book.Title;
        existingBook.Author = book.Author;
        existingBook.IsBorrowed = book.IsBorrowed;
    }

    public void Delete(Book book)
    {
        _books.Remove(book);
    }
}