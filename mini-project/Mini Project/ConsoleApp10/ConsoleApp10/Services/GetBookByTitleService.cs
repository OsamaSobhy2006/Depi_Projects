using LibraryManagementSystem.Models;
using LibraryManagementSystem.Repositories;

namespace LibraryManagementSystem.Services;

public class GetBookByTitleService
{
    private readonly BookRepository _bookRepository;

    public GetBookByTitleService(BookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public Book? GetBookByTitle(string title)
    {
        return _bookRepository.GetByTitle(title);
    }
}