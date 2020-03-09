import * as React from 'react';
import * as styles from './styles.less';
import { IconButton, AddIcon, DeleteIcon } from '@equinor/fusion-components';

type TaskbarProps = {
    onAddItem: () => void;
};

const Taskbar: React.FC<TaskbarProps> = ({onAddItem}) => {
    return (
        <div className={styles.taskBar}>
            <IconButton onClick={onAddItem}>
                <AddIcon />
            </IconButton>
            <IconButton>
                <DeleteIcon />
            </IconButton>
        </div>
    );
};

export default Taskbar;
