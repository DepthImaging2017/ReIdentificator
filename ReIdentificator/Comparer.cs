using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace ReIdentificator
{
    public class Comparer
    {
        private readonly int minimumSimiliarProperties = 5;
        private Database db;
        private MainWindow main;

        public Comparer(Database db, MainWindow main)
        {
            this.db = db;
            this.main = main;
        }

        public void compare(Individual current, List<Individual> all)
        {
            MatcherObject matcher = new MatcherObject();
            foreach(var person in all)
            {
                
                matcher.face_age = (current.face_age < (person.face_age + 2) && (current.face_age > person.face_age - 2));
                matcher.face_gender = (current.face_gender == person.face_gender);
                matcher.face_hair_bald = (current.face_hair_bald < (person.face_hair_bald + 10) && (current.face_hair_bald > person.face_hair_bald - 10));
                matcher.face_hair_blonde = (current.face_hair_blonde < (person.face_hair_blonde + 10) && (current.face_hair_blonde > person.face_hair_blonde - 10));
                matcher.face_hair_black = (current.face_hair_black < (person.face_hair_black + 10) && (current.face_hair_black > person.face_hair_black - 10));
                matcher.face_hair_brown = (current.face_hair_brown < (person.face_hair_brown + 10) && (current.face_hair_brown > person.face_hair_brown - 10));
                matcher.face_hair_red = (current.face_hair_red < (person.face_hair_red + 10) && (current.face_hair_red > person.face_hair_red - 10));
                matcher.face_glasses = (current.face_glasses == person.face_glasses);

                matcher.image_areacount_armleft = (current.image_areacount_armleft < (person.image_areacount_armleft + 10) && (current.image_areacount_armleft > person.image_areacount_armleft - 10));
                matcher.image_areacount_armright = (current.image_areacount_armright < (person.image_areacount_armright + 10) && (current.image_areacount_armright > person.image_areacount_armright - 10));
                matcher.image_areacount_legleft = (current.image_areacount_legleft < (person.image_areacount_legleft + 10) && (current.image_areacount_legleft > person.image_areacount_legleft - 10));
                matcher.image_areacount_legright = (current.image_areacount_legright < (person.image_areacount_legright + 10) && (current.image_areacount_legright > person.image_areacount_legright - 10));
                matcher.image_wears_watch = (current.image_wears_watch == person.image_wears_watch);
                matcher.image_wears_shirt = (current.image_wears_shirt == person.image_wears_shirt);
                matcher.image_wears_shorts = (current.image_wears_shorts == person.image_wears_shorts);                

                matcher.height = (current.height < (person.height + 0.05) && (current.height > person.height - 0.05));
                matcher.torsoHeight = (current.torsoHeight < (person.torsoHeight + 0.06) && (current.torsoHeight > person.torsoHeight - 0.06));
                matcher.neckToSpineMid = (current.neckToSpineMid < (person.neckToSpineMid + 0.04) && (current.neckToSpineMid > person.neckToSpineMid - 0.04));
                matcher.neckToLeftShoulder = (current.neckToLeftShoulder < (person.neckToLeftShoulder + 0.02) && (current.neckToLeftShoulder > person.neckToLeftShoulder - 0.02));
                matcher.neckToRightShoulder = (current.neckToRightShoulder < (person.neckToRightShoulder + 0.02) && (current.neckToRightShoulder > person.neckToRightShoulder - 0.02));
                matcher.leftHipToSpineBase = (current.leftHipToSpineBase < (person.leftHipToSpineBase + 0.01) && (current.leftHipToSpineBase > person.leftHipToSpineBase - 0.01));
                matcher.rightHipToSpineBase = (current.rightHipToSpineBase < (person.rightHipToSpineBase + 0.01) && (current.rightHipToSpineBase > person.rightHipToSpineBase - 0.01));
                matcher.spineMidToLeftShoulder = (current.spineMidToLeftShoulder < (person.spineMidToLeftShoulder + 0.02) && (current.spineMidToLeftShoulder > person.spineMidToLeftShoulder - 0.02));
                matcher.spineMidToRightShoulder = (current.spineMidToRightShoulder < (person.spineMidToRightShoulder + 0.02) && (current.spineMidToRightShoulder > person.spineMidToRightShoulder - 0.02));

                matcher.bodyWidth = (current.bodyWidth < (person.bodyWidth + 0.02) && (current.bodyWidth > person.bodyWidth - 0.1));

                int count = 0;
                PropertyInfo[] properties = matcher.GetType().GetProperties();
                foreach(PropertyInfo pi in properties)
                {
                    bool val = (bool)pi.GetValue(matcher, null);
                    if (val) count++;
                    Debug.WriteLine(pi.Name + " " + val);
                }
                if(count >= minimumSimiliarProperties)
                {
                    String times = "";
                    foreach(DateTime tm in person.timestamps)
                    {
                        times += ", " + tm.ToString();
                    }
                    main.printLog("Person was already here at" + times);
                    DateTime time = DateTime.Now;
                    person.timestamps.Add(time);
                    db.UpdateEntry(person.ID.ToString(), "timestamps", person.timestamps, null);
                    main.printLog("Person that left the frame is reidentified!");
                    return;
                }
            }
            db.AddEntry(current, null);
            main.printLog("Person that left the frame NOT reidentified!");
            return;

        } 

    }

    public class MatcherObject
    {
        public bool face_age { get; set; }
        public bool face_gender { get; set; }
        public bool face_hair_bald { get; set; }
        public bool face_hair_blonde { get; set; }
        public bool face_hair_black { get; set; }
        public bool face_hair_brown { get; set; }
        public bool face_hair_red { get; set; }
        public bool face_glasses { get; set; }

        public bool image_color_shoulderleft { get; set; }
        public bool image_color_shoulderright { get; set; }
        public bool image_color_torso { get; set; }
        public bool image_color_shoulderhistogram { get; set; }
        public bool image_color_spinehistogram { get; set; }
        public bool image_areacount_armleft { get; set; }
        public bool image_areacount_armright { get; set; }
        public bool image_areacount_legleft { get; set; }
        public bool image_areacount_legright { get; set; }
        public bool image_wears_watch { get; set; }
        public bool image_wears_shorts { get; set; }
        public bool image_wears_shirt { get; set; }

        public bool height { get; set; }
        public bool torsoHeight { get; set; }
        public bool neckToSpineMid { get; set; }
        public bool spineMidToSpineBase { get; set; }
        public bool neckToLeftShoulder { get; set; }
        public bool neckToRightShoulder { get; set; }
        public bool leftHipToSpineBase { get; set; }
        public bool rightHipToSpineBase { get; set; }
        public bool spineMidToLeftShoulder { get; set; }
        public bool spineMidToRightShoulder { get; set; }
        public bool bodyWidth { get; set; }
    }
}
