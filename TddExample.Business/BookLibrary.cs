﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace TddExample.Business
{
    public class BookLibrary
    {
        public const int MaxOutstandingLoans = 10;
        public static TimeSpan LoanDuration => TimeSpan.FromDays(14);

        private IBookLoanRepository _bookLoanRepository;

        public BookLibrary(IBookLoanRepository bookLoanRepository)
        {
            _bookLoanRepository = bookLoanRepository;
        }
        public async Task CheckoutBookAsync(string memberId, string isbn)
        {
            var outstandingBookLoans =
                (await _bookLoanRepository.GetOutstandingBookLoansForMemberAsync(memberId))
                .ToList();

            if ((outstandingBookLoans.Count + 1) > MaxOutstandingLoans)
            {
                throw new TooManyCheckedOutBooksException();
            }

            if (outstandingBookLoans.Any(x => x.DueDate <= DateTime.UtcNow))
            {
                throw new PastDueBooksException();
            }

            await CreateBookLoanForFirstAvailableCopy(memberId, isbn);
        }

        private async Task CreateBookLoanForFirstAvailableCopy(string memberId, string isbn)
        {
            var availableCopyIds = await _bookLoanRepository.GetAvailableCopyIdsAsync(isbn);
            if (!availableCopyIds.Any())
            {
                throw new NoCopiesAvailableException();
            }

            bool checkoutSuccessful = false;
            foreach (var copyId in availableCopyIds)
            {
                checkoutSuccessful = await _bookLoanRepository.TryCreateBookLoanAsync(
                    new BookLoan
                    {
                        MemberId = memberId,
                        Isbn = isbn,
                        CopyId = copyId,
                        DueDate = DateTime.UtcNow.Date + LoanDuration,
                        WasReturned = false
                    });
                if (checkoutSuccessful)
                {
                    break;
                }
            }

            if (!checkoutSuccessful)
            {
                throw new NoCopiesAvailableException();
            }
        }
    }
}
