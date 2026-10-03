using System;
using System.Collections.Generic;
using System.Linq;

public class Library
{
    private List<Book> books = new List<Book>();

    // Create Book
    public void CreateBook(Book book)
    {
        books.Add(book);
    }

    // Get All Books
    public List<Book> GetAllBooks()
    {
        return books;
    }

    // Get Book By Title
    public Book GetBookByTitle(string title)
    {
        return books.FirstOrDefault(b =>
            b.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }
}