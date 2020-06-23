import Personnel from '../../../../../../../../models/Personnel';

type PersonnelLine = Personnel & {
    selected?: boolean;
};

export default PersonnelLine;
