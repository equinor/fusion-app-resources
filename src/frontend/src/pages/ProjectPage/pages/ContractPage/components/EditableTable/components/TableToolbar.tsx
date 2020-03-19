import * as React from 'react';
import {
    IconButton,
    useDropdownController,
    Dropdown,
    CloseIcon,
    MoreIcon,
} from '@equinor/fusion-components';
import * as styles from '../styles.less';

type TableToolbarProps = {
    onRemove?: () => void;
};

const TableToolbar: React.FC<TableToolbarProps> = ({ onRemove }) => {
    const dropdownController = useDropdownController((_, isOpen, setIsOpen) => (
        <IconButton onClick={() => setIsOpen(!isOpen)}>
            <MoreIcon />
        </IconButton>
    ));

    const containerRef = dropdownController.controllerRef as React.MutableRefObject<HTMLDivElement | null>;

    const { isOpen, setIsOpen } = dropdownController;
    const select = React.useCallback(
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
