export function clickOnEnter(userMessage,sendBtn) {
    userMessage.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.ctrlKey && !e.shiftKey && !e.altKey && !e.metaKey) {
            // sendBtn.click();
            sendBtn.dispatchEvent(new Event('click'))
        }
    });
}
