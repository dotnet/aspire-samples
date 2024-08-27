export function submitOnEnter(formElem) {
    formElem.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.ctrlKey && !e.shiftKey && !e.altKey && !e.metaKey) {
            e.srcElement.dispatchEvent(new Event('change', { bubbles: true }));
            formElem.requestSubmit();
        }
    });

    formElem.addEventListener('submit', e => {
        // If you're nearly scrolled to the end, scroll entirely to the end so that
        // when new content is added it will auto-scroll and follow it
        const messagesScroller = document.querySelector('.messages-scroller');
        if (Math.abs(messagesScroller.scrollTop) < 5) {
            messagesScroller.scrollTop = 0;
        }
    });
}