import * as React from 'react';
import * as styles from './styles.less';
import { Button, IconButton, DeleteIcon, EditIcon } from '@equinor/fusion-components';

const ActualMppPage: React.FC = () => {
    return (
        <div className={styles.actualMppContainer}>
            <div className={styles.toolbar}>
                <Button>Request personnel</Button>
                <div>
                    <IconButton>
                        <DeleteIcon />
                    </IconButton>
                    <IconButton>
                        <EditIcon />
                    </IconButton>
                </div>
            </div>
        </div>
    );
};

export default ActualMppPage;
