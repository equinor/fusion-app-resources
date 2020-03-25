import * as React from 'react';
import { ModalSideSheet, Button, AddIcon, EditIcon } from '@equinor/fusion-components';
import * as styles from '../styles.less';
import { Position } from '@equinor/fusion';
import Contract from '../../../../../models/contract';
import ExternalPositionSidesheet from './ExternalPositionSidesheet';

type CreateOrEditExternalPositionButtonProps = {
    contract: Contract;
    onComplete: (position: Position) => void;
    repType: 'company-rep' | 'contract-responsible';
    existingPosition: Position | null;
};

const CreateOrEditExternalPositionButton: React.FC<CreateOrEditExternalPositionButtonProps> = ({
    repType,
    contract,
    onComplete,
    existingPosition,
}) => {
    const [isShowing, setIsShowing] = React.useState(false);

    const show = React.useCallback(() => setIsShowing(true), []);
    const onClose = React.useCallback(() => {
        setIsShowing(false);
    }, []);

    return (
        <>
            {existingPosition ? (
                <div className={styles.helpText}>
                    <Button frameless onClick={show}>
                        <EditIcon />{' '}
                        <span className={styles.buttonPositionName}>
                            Edit {existingPosition.name}
                        </span>
                    </Button>
                </div>
            ) : (
                <div className={styles.helpText}>
                    <span>If you can't find your position, try to </span>
                    <Button frameless onClick={show}>
                        <AddIcon /> Add new position
                    </Button>
                </div>
            )}
            <ModalSideSheet
                show={isShowing}
                onClose={onClose}
                size="large"
                header={
                    existingPosition
                        ? `Edit ${existingPosition.name}`
                        : 'Add new position to contract'
                }
            >
                <ExternalPositionSidesheet
                    contract={contract}
                    existingPosition={existingPosition}
                    onClose={onClose}
                    onComplete={onComplete}
                    repType={repType}
                />
            </ModalSideSheet>
        </>
    );
};

export default CreateOrEditExternalPositionButton;
