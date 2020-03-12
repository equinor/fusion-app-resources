import * as React from 'react';
import { IconButton, AddIcon, EditIcon, DeleteIcon } from '@equinor/fusion-components';
import * as styles from './styles.less';

export type IconButtonProps = {
    disabled?: boolean;
    onClick: (e: React.MouseEvent<HTMLButtonElement>) => void;
};

export type ToolBarProps = {
    addButton?: IconButtonProps,
    deleteButton?: IconButtonProps,
    editButton?: IconButtonProps,
};

const ManagePersonnelToolBar: React.FC<ToolBarProps> = ({ addButton, deleteButton, editButton }) => {
    return <div className={styles.container}>
        <IconButton onClick={addButton?.onClick} disabled={addButton ? addButton.disabled : true} ><AddIcon /></IconButton>
        <IconButton onClick={deleteButton?.onClick} disabled={deleteButton ? deleteButton.disabled : true}><DeleteIcon /></IconButton>
        <IconButton onClick={editButton?.onClick} disabled={editButton ? editButton.disabled : true}><EditIcon /></IconButton>
    </div>;
};

export default ManagePersonnelToolBar;
