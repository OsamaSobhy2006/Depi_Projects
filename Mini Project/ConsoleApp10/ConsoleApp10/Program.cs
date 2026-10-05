using LibraryManagementSystem;
using LibraryManagementSystem.Repositories;
using LibraryManagementSystem.Services;

var bookRepository = new BookRepository();

var createBookService = new CreateBookService(bookRepository);
var getAllBooksService = new GetAllBooksService(bookRepository);
var getBookByTitleService = new GetBookByTitleService(bookRepository);
var updateBookService = new UpdateBookService(bookRepository);
var deleteBookService = new DeleteBookService(bookRepository);
var borrowBookService = new BorrowBookService(bookRepository);
var lateFeeService = new LateFeeService();

var returnBookService = new ReturnBookService(
    bookRepository,
    lateFeeService);

var menu = new LibraryMenu(
    createBookService,
    getAllBooksService,
    getBookByTitleService,
    updateBookService,
    deleteBookService,
    borrowBookService,
    returnBookService
);

menu.Show();
