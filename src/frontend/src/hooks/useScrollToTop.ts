import { useState, useRef, useEffect, useCallback } from 'react';

const useScrollToTop = <T extends HTMLElement>(hasScrolledActiveLimit: number = 100) => {
    const [hasScrolled, setHasScrolled] = useState(Boolean);
    const scrollRef = useRef<T | null>(null);

    useEffect(() => {
        if (scrollRef.current === null) return;
        scrollRef.current?.addEventListener('scroll', onScroll);

        return () => {
            scrollRef.current?.removeEventListener('scroll', onScroll);
        };
    }, [scrollRef.current, hasScrolled]);

    const onScroll = useCallback(() => {
        if (scrollRef.current === null) return;

        if (hasScrolled && scrollRef.current.scrollTop < hasScrolledActiveLimit)
            setHasScrolled(false);

        if (!hasScrolled && scrollRef.current.scrollTop >= hasScrolledActiveLimit)
            setHasScrolled(true);
    }, [hasScrolled]);

    const scrollToTop = useCallback(() => {
        if (scrollRef.current === null) return;
        scrollRef.current.scrollTop = 0;
        setHasScrolled(false);
    }, []);

    return {
        scrollRef,
        hasScrolled,
        scrollToTop,
    };
};

export default useScrollToTop;
