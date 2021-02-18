
import {
    IconButton,
    useDropdownController,
    Dropdown,
    CloseIcon,
    MoreIcon,
    CopyIcon,
} from '@equinor/fusion-components';
import { FC, MutableRefObject, useCallback } from 'react';
import styles from '../styles.less';

type TableToolbarProps = {
    onRemove?: () => void;
    onCopy?: () => void;
};

const TableToolbar: FC<TableToolbarProps> = ({ onRemove, onCopy }) => {
    const dropdownController = useDropdownController((_, isOpen, setIsOpen) => (
        <IconButton onClick={() => setIsOpen(!isOpen)}>
            <MoreIcon />
        </IconButton>
    ));

    const containerRef = dropdownController.controllerRef as MutableRefObject<HTMLDivElement | null>;

    const { isOpen, setIsOpen } = dropdownController;
    const select = useCallback(
        (onClick?: () => void) => {
            onClick && onClick();
            if (isOpen) {
                setIsOpen(false);
            }
        },
        [isOpen]
    );

    return (
        <div ref={containerRef}>
            <Dropdown controller={dropdownController}>
                <div className={styles.menuContainer}>
                    <div className={styles.menuItem} key={'copy'} onClick={() => select(onCopy)}>
                        <div className={styles.icon}>
                            <CopyIcon />
                        </div>
                        <span className={styles.label}>Copy</span>
                    </div>
                    <div
                        className={styles.menuItem}
                        key={'remove'}
                        onClick={() => select(onRemove)}
                    >
                        <div className={styles.icon}>
                            <CloseIcon />
                        </div>
                        <span className={styles.label}>Remove</span>
                    </div>
                </div>
            </Dropdown>
        </div>
    );
};

export default TableToolbar;
