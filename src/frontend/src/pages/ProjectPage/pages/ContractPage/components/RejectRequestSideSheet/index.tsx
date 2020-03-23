import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { ModalSideSheet, TextArea, Button } from '@equinor/fusion-components';
import * as styles from './styles.less';

type RejectRequestSideSheetProps = {
    requests: PersonnelRequest[];
    setRequests: (requests: PersonnelRequest[]) => void;
    onReject: (reason: string) => void;
};

const RejectPersonnelSideSheet: React.FC<RejectRequestSideSheetProps> = ({
    requests,
    setRequests,
    onReject,
}) => {
    const [inputValue, setInputValue] = React.useState<string>('');

    return (
        <ModalSideSheet
            show={requests.length > 0}
            size="medium"
            id="reject-personnel-side-sheet"
            onClose={() => setRequests([])}
            header={`Reject ${requests.length} requests?`}
            headerIcons={[
                <Button
                    disabled={inputValue.length <= 0}
                    outlined
                    onClick={() => {
                        setRequests([]);
                        onReject(inputValue);
                    }}
                >
                    Confirm
                </Button>,
            ]}
            safeClose
            safeCloseTitle="Close reject side sheet?  Rejections will not be completed"
            safeCloseCancelLabel={'Continue editing'}
            safeCloseConfirmLabel={'Discard changes'}
        >
            <div className={styles.container}>
                <TextArea
                    value={inputValue}
                    onChange={setInputValue}
                    helperText={'Describe the reason why you are rejecting  the request'}
                    placeholder="Please provide a reason for rejection"
                />
            </div>
        </ModalSideSheet>
    );
};

export default RejectPersonnelSideSheet;
