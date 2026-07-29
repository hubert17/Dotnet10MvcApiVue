window.gabsScroll = {
    initHorizontalScroll: (elementId) => {
        const element = document.getElementById(elementId);
        if (!element) return;

        element.addEventListener('wheel', (e) => {
            if (e.deltaY !== 0) {
                e.preventDefault();
                element.scrollLeft += e.deltaY;
            }
        }, { passive: false });

        let isDown = false;
        let startX;
        let scrollLeft;
        let hasDragged = false;

        element.style.cursor = 'grab';

        element.addEventListener('mousedown', (e) => {
            isDown = true;
            hasDragged = false;
            element.style.cursor = 'grabbing';
            element.style.userSelect = 'none';
            startX = e.pageX - element.offsetLeft;
            scrollLeft = element.scrollLeft;
        });

        element.addEventListener('mouseleave', () => {
            if (isDown) {
                isDown = false;
                element.style.cursor = 'grab';
                element.style.removeProperty('user-select');
            }
        });

        element.addEventListener('mouseup', () => {
            if (isDown) {
                isDown = false;
                element.style.cursor = 'grab';
                element.style.removeProperty('user-select');
            }
        });

        element.addEventListener('mousemove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            const x = e.pageX - element.offsetLeft;
            const walk = (x - startX) * 1.5;
            if (Math.abs(walk) > 5) {
                hasDragged = true;
            }
            element.scrollLeft = scrollLeft - walk;
        });

        element.addEventListener('click', (e) => {
            if (hasDragged) {
                e.preventDefault();
                e.stopPropagation();
                hasDragged = false;
            }
        }, true);
    }
};
