import * as React from 'react';
import * as styles from './styles.less';
import { IconButton, AddIcon, DeleteIcon, useTooltipRef } from '@equinor/fusion-components';

type TaskbarProps = {
    onAddItem: () => void;
};

const Taskbar: React.FC<TaskbarProps> = ({onAddItem}) => {
    const addItemTooltipRef = useTooltipRef("Add item");
    const removeItemTooltipRef = useTooltipRef("Remove item");

    return (
        <div className={styles.taskBar}>
            <IconButton onClick={onAddItem} ref={addItemTooltipRef}>
                <AddIcon />
            </IconButton>
            <IconButton ref={removeItemTooltipRef}>
                <DeleteIcon />
            </IconButton>
        </div>
    );
};

export default Taskbar;
