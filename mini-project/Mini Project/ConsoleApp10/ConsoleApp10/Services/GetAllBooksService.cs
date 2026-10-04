using LibraryManagementSystem.Models;
using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services;

public class GetAllBooksService
{
    private readonly BookRepository _bookRepository;

    public GetAllBooksService(BookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public List<Book> GetAllBooks()
    {
        return _bookRepository.GetAll();
    }
}