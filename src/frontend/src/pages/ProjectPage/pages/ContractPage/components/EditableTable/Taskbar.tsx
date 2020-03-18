import * as React from 'react';
import * as styles from './styles.less';
import { IconButton, AddIcon, useTooltipRef, CloseIcon } from '@equinor/fusion-components';

type TaskbarProps<T> = {
    onAddItem: () => void;
    onRemoveItem: (removeItems: T[]) => void;
    selectedItems: T[];
};

function Taskbar<T>({ onAddItem, onRemoveItem, selectedItems }: TaskbarProps<T>) {
    const addItemTooltipRef = useTooltipRef('Add item');
    const removeItemTooltipRef = useTooltipRef('Remove item');

    return (
        <div className={styles.taskBar}>
            <IconButton onClick={onAddItem} ref={addItemTooltipRef}>
                <AddIcon />
            </IconButton>
            <IconButton
                ref={removeItemTooltipRef}
                disabled={selectedItems.length <= 0}
                onClick={() => onRemoveItem(selectedItems)}
            >
                <CloseIcon />
            </IconButton>
        </div>
    );
}

export default Taskbar;
